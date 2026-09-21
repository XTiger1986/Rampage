using Rampage.Reflection;
using System;
using System.Net;
using System.Reflection;

namespace Rampage.Net
{
    /// <summary>
    /// HTTP 请求选项：承载请求方法、输入数据、请求头、Cookie、拦截器等通用请求参数
    /// HTTP request options carrying the method, input data, headers, cookies, interceptor and other common request parameters
    /// </summary>
    public class SuperRequestOptions
    {
        /// <summary>构造并初始化 InputData/Headers/Cookies / Constructs with InputData/Headers/Cookies initialized</summary>
        public SuperRequestOptions()
        {
            InputData = new Dictionary<string, object?>();
            Headers = new Dictionary<string, string>();
            Cookies = new CookieCollection();
        }

        /// <summary>HTTP 请求方法，默认 GET / The HTTP method, GET by default</summary>
        public virtual HttpMethod Method { get; set; } = HttpMethod.Get;

        /// <summary>请求/响应拦截器 / The request/response interceptor</summary>
        public ISuperClientInterceptor? Handler { get; set; }

        /// <summary>关联的客户端配置（会传递给拦截器）/ The associated client config (passed to the interceptor)</summary>
        public SuperClientConfig? Config { get; set; }

        private IDictionary<string, object?>? _inputData;
        /// <summary>
        /// 请求数据字典：未显式设置且 InputObject 非空时，首次访问反射 InputObject 的属性生成（跳过 null 属性并缓存结果）
        /// The request data dictionary: when not set explicitly and InputObject is non-null, the first access reflects InputObject's properties into it (null properties skipped, result cached)
        /// </summary>
        /// <remarks>
        /// 两者皆无时每次访问返回新的空字典（不缓存）；字典内容的去向（query string/表单/JSON body）由请求扩展按 Method 与 UseBody 决定。
        /// When neither is present, every access returns a new empty dictionary (uncached); where the data goes (query string/form/JSON body) is decided by the request extensions based on Method and UseBody.
        /// </remarks>
        public virtual IDictionary<string, object?> InputData
        {
            get
            {
                if (_inputData == null)
                {
                    if (InputObject != null)
                    {
                        _inputData = new Dictionary<string, object?>();
                        List<PropertyInfo> prpList = ReflectionTool.GetPropertyList(InputObject.GetType());
                        foreach (PropertyInfo prp in prpList)
                        {
                            object? prpVal = EmitTool.GetPropertyValue(InputObject, prp.Name);
                            if (prpVal != null)
                            {
                                _inputData.Add(prp.Name, prpVal);
                            }
                        }
                    }
                }
                return _inputData ?? new Dictionary<string, object?>();
            }
            set
            {
                _inputData = value;
            }
        }


        /// <summary>请求头字典 / The request header dictionary</summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>请求 Cookie 集合（发送时手工拼接为 Cookie 请求头）/ The request cookie collection (manually joined into the Cookie request header when sending)</summary>
        public CookieCollection Cookies { get; set; }

        /// <summary>是否将 InputData 整体序列化为 JSON body 提交（为 true 时不再拼 query string/表单），默认 false / Whether to serialize InputData as a JSON body (skips query string/form when true); false by default</summary>
        public virtual bool UseBody { get; set; } = false;

        /// <summary>输入模型对象：仅在 InputData 未显式设置时用于反射生成数据字典 / The input model object: used to reflect the data dictionary only when InputData is not set explicitly</summary>
        public object? InputObject { get; set; }

    }

    /// <summary>
    /// 强类型 HTTP 请求选项：以类型参数 T 的属性列表反射 InputObject 生成请求数据
    /// Strongly-typed HTTP request options: reflects InputObject using the property list of type parameter T to build the request data
    /// </summary>
    /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
    public class HttpRequestOptions<T> : SuperRequestOptions
    {
        private IDictionary<string, object?>? _inputData;
        /// <summary>
        /// 请求数据字典：未显式设置且 InputObject 非空时，按类型 T（而非运行时类型）的属性列表反射生成并缓存
        /// The request data dictionary: when not set explicitly and InputObject is non-null, built and cached from the property list of type T (not the runtime type)
        /// </summary>
        public override IDictionary<string, object?> InputData
        {
            get
            {
                if (_inputData == null)
                {
                    if (InputObject != null)
                    {
                        _inputData = new Dictionary<string, object?>();
                        List<PropertyInfo> prpList = ReflectionTool.GetPropertyList(typeof(T));
                        foreach (PropertyInfo prp in prpList)
                        {
                            object? prpVal = EmitTool.GetPropertyValue(InputObject, prp.Name);
                            if (prpVal != null)
                            {
                                _inputData.Add(prp.Name, prpVal);
                            }
                        }
                    }
                }
                return _inputData ?? new Dictionary<string, object?>();
            }
            set { _inputData = value; }
        }

        /// <summary>强类型输入模型对象（隐藏基类同名属性）/ The strongly-typed input model object (hides the base-class property)</summary>
        public new T? InputObject { get; set; }

    }

    /// <summary>
    /// multipart 表单请求选项：请求方法固定为 POST，并附带上传文件集合
    /// Multipart form request options: the method is fixed to POST, with an upload file collection attached
    /// </summary>
    public class HttpRequestMultipartOptions : SuperRequestOptions
    {
        /// <summary>HTTP 请求方法：固定返回 POST / The HTTP method: always returns POST</summary>
        public override HttpMethod Method { get => HttpMethod.Post; }

        /// <summary>上传文件集合：键为表单字段名 / The upload files keyed by form field name</summary>
        public virtual Dictionary<string, MultipartFileInfo>? UploadFiles { get; set; }

        /// <summary>是否 JSON body 提交：固定返回 false / Whether to use a JSON body: always returns false</summary>
        public override bool UseBody { get => false; }

    }

    /// <summary>
    /// 强类型 multipart 表单请求选项：以类型参数 T 的属性列表反射 InputObject 生成请求数据
    /// Strongly-typed multipart form request options: reflects InputObject using the property list of type parameter T to build the request data
    /// </summary>
    /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
    public class HttpRequestMultipartOptions<T> : HttpRequestMultipartOptions
    {
        private IDictionary<string, object?>? _inputData;
        /// <summary>
        /// 请求数据字典：未显式设置且 InputObject 非空时，按类型 T（而非运行时类型）的属性列表反射生成并缓存
        /// The request data dictionary: when not set explicitly and InputObject is non-null, built and cached from the property list of type T (not the runtime type)
        /// </summary>
        public override IDictionary<string, object?> InputData
        {
            get
            {
                if (_inputData == null)
                {
                    if (InputObject != null)
                    {
                        _inputData = new Dictionary<string, object?>();
                        List<PropertyInfo> prpList = ReflectionTool.GetPropertyList(typeof(T));
                        foreach (PropertyInfo prp in prpList)
                        {
                            object? prpVal = EmitTool.GetPropertyValue(InputObject, prp.Name);
                            if (prpVal != null)
                            {
                                _inputData.Add(prp.Name, prpVal);
                            }
                        }
                    }
                }
                return _inputData ?? new Dictionary<string, object?>();
            }
            set {  _inputData = value; }
        }

        /// <summary>强类型输入模型对象（隐藏基类同名属性）/ The strongly-typed input model object (hides the base-class property)</summary>
        public new T? InputObject { get; set; }

    }

}
