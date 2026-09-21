using Rampage.Log;
using Rampage.Reflection;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Rampage.Data
{
    /// <summary>
    /// 实体-表映射模式的静态存储
    /// Static store of entity-table mapping schemas
    /// </summary>
    /// <remarks>
    /// 类型首次被访问时，静态构造函数调用 <see cref="Initialize"/> 扫描 RampageBooter.EssentialAssemblies
    /// 中标注了 <see cref="TableMapping"/> 的类型并缓存解析结果（失败仅记日志，不中断启动）。
    /// 未注册的类型在首次获取映射时按“类型名=表名、属性名=列名”约定懒解析并写入缓存。
    /// 两个实体类型映射到同一张表名时抛出异常。
    /// </remarks>
    public class ModelSchemaStore
    {
        /// <summary>
        /// 初始化与懒解析共用的全局锁
        /// Global lock shared by Initialize and lazy schema parsing
        /// </summary>
        private static object _storeLock = new object();
        /// <summary>
        /// 静态构造函数：启动时自动扫描 EssentialAssemblies 预加载实体映射，
        /// 任何异常仅记录错误日志，不会中断程序启动
        /// Static constructor: preloads schemas by scanning EssentialAssemblies;
        /// any exception is logged instead of breaking startup
        /// </summary>
        static ModelSchemaStore()
        {
            try
            {
                Initialize();
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
            }
        }

        /// <summary>
        /// 实体映射缓存，键为实体类型全名（Type.FullName）
        /// Schema cache keyed by entity type full name (Type.FullName)
        /// </summary>
        public static ConcurrentDictionary<string, TableMapping> SchemaDict
        {
            get;
        } = new ConcurrentDictionary<string, TableMapping>();

        /// <summary>
        /// 表名到实体类型的索引，键为数据表名（表名重复会在注册时抛异常）
        /// Table-name to entity-type index, keyed by table name (duplicate names throw at registration)
        /// </summary>
        public static ConcurrentDictionary<string, Type> TableTypeRelationship
        {
            get;
        } = new ConcurrentDictionary<string, Type>();

        /// <summary>
        /// 扫描程序集解析实体映射并写入缓存（可重复调用，结果幂等）
        /// Scan assemblies, resolve entity schemas and cache them (repeatable and idempotent)
        /// </summary>
        /// <param name="ns">只扫描这些命名空间下的类型；未指定时仅加载标注了 TableMapping 的类型 / only scan types in these namespaces; when omitted, only TableMapping-annotated types are loaded</param>
        /// <param name="assemblies">限定扫描的程序集；未指定时扫描 EssentialAssemblies / assemblies to scan; defaults to RampageBooter.EssentialAssemblies</param>
        /// <exception cref="ArgumentException">两个实体类型映射到同一张表时抛出 / thrown when two entity types map to the same table name</exception>
        public static void Initialize(ICollection<string>? ns = null, Assembly[]? assemblies = null)
        {
            lock (_storeLock)
            {
                if (assemblies == null || assemblies.Length == 0)
                {
                    assemblies = RampageBooter.EssentialAssemblies.ToArray();
                }

                if (assemblies.Length > 0)
                {
                    foreach (Assembly assembly in assemblies)
                    {
                        Type[] types = assembly.GetTypes();
                        if (types != null && types.Length > 0)
                        {
                            foreach (Type type in types)
                            {
                                if (ns != null && ns.Count > 0 && type.Namespace != null && false == ns.Contains(type.Namespace))
                                    continue;
                                if ((ns == null || ns.Count == 0) && false == type.IsDefined(typeof(TableMapping), true))
                                    continue;

                                TableMapping schema = GetModelMappingFromType(type);

                                SchemaDict.TryAdd(schema.ModelType!.FullName!, schema);
                                if (!TableTypeRelationship.TryAdd(schema.TableName!, schema.ModelType!))
                                {
                                    throw new ArgumentException($"Duplicate table name mapping: {schema.TableName} (type: {schema.ModelType.FullName})");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 解析单个实体类型的映射信息：读取类级 TableMapping 与属性级 FieldMapping 特性，
        /// 未标注特性的按“类型名=表名、属性名=列名”约定处理，并校验主键唯一性
        /// Resolve the schema of one entity type: reads the class-level TableMapping and
        /// property-level FieldMapping attributes, falls back to type-name/property-name
        /// conventions, and validates primary-key uniqueness
        /// </summary>
        /// <param name="type">实体类型 / the entity type</param>
        /// <returns>解析出的映射信息 / the resolved schema</returns>
        /// <exception cref="Exception">实体声明了多个不同的主键字段（联合主键）时抛出 / thrown when the entity declares more than one distinct primary key</exception>
        private static TableMapping GetModelMappingFromType(Type type)
        {
            string typeName = type.Name;
            TableMapping? modelMapping = null;
            if (type.IsDefined(typeof(TableMapping)))
                modelMapping = (TableMapping)(type.GetCustomAttributes(typeof(TableMapping), true)[0]);

            if (modelMapping == null)
            {
                modelMapping = new TableMapping
                {
                    TableName = typeName
                };
            }
            else
            {
                if (string.IsNullOrEmpty(modelMapping.TableName))
                    modelMapping.TableName = typeName;
            }

            modelMapping.ModelType = type;
            modelMapping.PropertyMappingDict = new Dictionary<string, FieldMapping>();
            modelMapping.ColumnMappingDict = new Dictionary<string, FieldMapping>();

            foreach (PropertyInfo prpInfo in type.GetProperties())
            {
                string columnName = prpInfo.Name;
                FieldMapping? prpMapping = null;

                if (prpInfo.IsDefined(typeof(FieldMapping), true))
                    prpMapping = (FieldMapping)(prpInfo.GetCustomAttributes(typeof(FieldMapping), true)[0]);

                if (prpMapping == null)
                {
                    prpMapping = new FieldMapping
                    {
                        TableName = modelMapping.TableName,
                        ColumnName = columnName,
                        NotColumn = false,
                        AutoIncrement = false,
                        PrimaryKey = false
                    };
                }
                else
                {
                    if (!string.IsNullOrEmpty(prpMapping.ColumnName))
                    {
                        columnName = prpMapping.ColumnName;
                    }
                    else
                    {
                        prpMapping.ColumnName = columnName;
                    }

                    if (prpMapping.PrimaryKey)
                    {
                        if (!string.IsNullOrEmpty(modelMapping.PrimaryKey) && string.Compare(modelMapping.PrimaryKey, columnName, true) != 0)
                        {
                            throw new Exception("union primary key is not supported currently!");
                        }
                        modelMapping.PrimaryKey = columnName;
                    }
                }

                prpMapping.Property = prpInfo;
                modelMapping.PropertyMappingDict.Add(prpInfo.Name, prpMapping);
                if (false == prpMapping.NotColumn)
                {
                    modelMapping.ColumnMappingDict.Add(prpMapping.ColumnName, prpMapping);
                }
            }

            return modelMapping;
        }


        /// <summary>
        /// 获取实体类型对应的数据表名（未注册类型会按需懒解析）
        /// Get the table name of an entity type (unregistered types are parsed on demand)
        /// </summary>
        /// <typeparam name="M">实体类型 / entity type</typeparam>
        /// <returns>数据表名 / the table name</returns>
        public static string? GetTableName<M>()
        {
            Type type = typeof(M);
            return GetTableName(type);
        }

        /// <summary>
        /// 获取实体类型对应的数据表名（未注册类型会按需懒解析）
        /// Get the table name of an entity type (unregistered types are parsed on demand)
        /// </summary>
        /// <param name="type">实体类型 / entity type</param>
        /// <returns>数据表名 / the table name</returns>
        public static string? GetTableName(Type type)
        {
            TableMapping? schema = GetModelMapping(type);
            if (schema == null)
                return null;
            return schema.TableName;
        }

        /// <summary>
        /// 获取实体类型的主键字段名（未注册类型会按需懒解析）
        /// Get the primary key column of an entity type (unregistered types are parsed on demand)
        /// </summary>
        /// <typeparam name="M">实体类型 / entity type</typeparam>
        /// <returns>主键字段名；实体未声明主键时为 null / the primary key column, or null when the entity declares none</returns>
        public static string? GetPrimaryKey<M>() where M : class
        {
            Type type = typeof(M);
            return GetPrimaryKey(type);
        }

        /// <summary>
        /// 获取实体类型的主键字段名（未注册类型会按需懒解析）
        /// Get the primary key column of an entity type (unregistered types are parsed on demand)
        /// </summary>
        /// <param name="type">实体类型 / entity type</param>
        /// <returns>主键字段名；实体未声明主键时为 null / the primary key column, or null when the entity declares none</returns>
        public static string? GetPrimaryKey(Type type)
        {
            TableMapping? schema = GetModelMapping(type);
            if (schema == null)
                return null;
            return schema.PrimaryKey;
        }

        /// <summary>
        /// 按数据表名获取主键字段名（仅查找已注册的映射，不做懒解析）
        /// Get the primary key column by table name (registered schemas only; no lazy parsing)
        /// </summary>
        /// <param name="tableName">数据表名 / table name</param>
        /// <returns>主键字段名；表名未注册时返回 null / the primary key column, or null when the table is unknown</returns>
        public static string? GetPrimaryKey(string tableName)
        {
            TableMapping? schema = GetModelMapping(tableName);
            if (schema == null)
                return null;
            return schema.PrimaryKey;
        }

        /// <summary>
        /// 获取实体类型的映射信息（未注册类型会按需懒解析并缓存）
        /// Get the schema of an entity type (unregistered types are parsed and cached on demand)
        /// </summary>
        /// <typeparam name="M">实体类型 / entity type</typeparam>
        /// <returns>映射信息 / the schema</returns>
        public static TableMapping? GetModelMapping<M>() where M : class
        {
            Type type = typeof(M);
            return GetModelMapping(type);
        }

        /// <summary>
        /// 获取实体类型的映射信息；缓存未命中时在锁内懒解析并写入缓存（双检锁）
        /// Get the schema of a type; lazily parses and caches it under a double-checked lock on first miss
        /// </summary>
        /// <param name="type">实体类型 / entity type</param>
        /// <returns>映射信息 / the schema</returns>
        public static TableMapping? GetModelMapping(Type type)
        {
            string typeFullName = type.FullName!;
            if (!SchemaDict.TryGetValue(typeFullName, out TableMapping? schema))
            {
                lock (_storeLock)
                {
                    if (!SchemaDict.TryGetValue(typeFullName, out schema))
                    {
                        schema = GetModelMappingFromType(type);
                        SchemaDict.TryAdd(schema.ModelType!.FullName!, schema);
                        TableTypeRelationship.TryAdd(schema.TableName!, schema.ModelType!);
                    }
                }
            }
            return schema;
        }

        /// <summary>
        /// 按数据表名获取映射信息（仅查找已注册的映射，不做懒解析）
        /// Get a schema by table name (registered schemas only; no lazy parsing)
        /// </summary>
        /// <param name="tableName">数据表名 / table name</param>
        /// <returns>映射信息；表名未注册时返回 null / the schema, or null when the table is unknown</returns>
        public static TableMapping? GetModelMapping(string tableName)
        {
            if (!TableTypeRelationship.ContainsKey(tableName))
            {
                return null;
            }
            Type modelType = TableTypeRelationship[tableName];
            TableMapping? schema = SchemaDict[modelType.FullName!];
            return schema;
        }

    }
}
