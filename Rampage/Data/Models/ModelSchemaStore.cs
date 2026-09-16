using Rampage.Log;
using Rampage.Reflection;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Rampage.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class ModelSchemaStore
    {
        /// <summary>
        /// 
        /// </summary>
        private static object _storeLock = new object();
        /// <summary>
        /// 
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
        ///
        /// </summary>
        public static ConcurrentDictionary<string, TableMapping> SchemaDict
        {
            get;
        } = new ConcurrentDictionary<string, TableMapping>();

        /// <summary>
        ///
        /// </summary>
        public static ConcurrentDictionary<string, Type> TableTypeRelationship
        {
            get;
        } = new ConcurrentDictionary<string, Type>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ns">Automatically search for entity types from the specified namespace (if not specified, load from annotated ModelMapping classes)</param>
        /// <param name="assemblies">Automatically search for entity types from the specified assembly (if not specified, load from all assemblies)</param>
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
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <returns></returns>
        public static string? GetTableName<M>()
        {
            Type type = typeof(M);
            return GetTableName(type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string? GetTableName(Type type)
        {
            TableMapping? schema = GetModelMapping(type);
            if (schema == null)
                return null;
            return schema.TableName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <returns></returns>
        public static string? GetPrimaryKey<M>() where M : class
        {
            Type type = typeof(M);
            return GetPrimaryKey(type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string? GetPrimaryKey(Type type)
        {
            TableMapping? schema = GetModelMapping(type);
            if (schema == null)
                return null;
            return schema.PrimaryKey;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string? GetPrimaryKey(string tableName)
        {
            TableMapping? schema = GetModelMapping(tableName);
            if (schema == null)
                return null;
            return schema.PrimaryKey;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <returns></returns>
        public static TableMapping? GetModelMapping<M>() where M : class
        {
            Type type = typeof(M);
            return GetModelMapping(type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
