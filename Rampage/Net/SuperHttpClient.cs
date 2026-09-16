using Rampage.ExceptionExtensions;
using Rampage.Reflection;
using Rampage.Utilities;
using System;
using System.Collections;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Rampage.Net
{
    public abstract class SuperHttpClient : ISuperHttpClient
    {

        public const string EX_KEY_RequestFormFileAttribute_Parameter_Not_Allowed = "RequestFormFileAttribute_Parameter_Not_Allowed";
        public const string EX_KEY_RequestBodyAttribute_Parameter_Not_Allowed = "RequestBodyAttribute_Parameter_Not_Allowed";
        public const string EX_KEY_Invalid_RequestFormFileAttribute_Parameter_Value = "Invalid_RequestFormFileAttribute_Parameter_Value";


        public SuperHttpClient()
        {
        }

        public virtual void TryLoadConfig()
        {
            if (ClientAttribute == null)
            {
                return;
            }

            string? name = ClientAttribute.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                Type clientType = this.GetType();
                name = clientType.Name;
            }
            if (Config == null)
            {
                Config = SuperClientConfigManager.GetConfig(name);
            }

            if (string.IsNullOrEmpty(ApiRoot))
            {
                string? root = ClientAttribute.Root;
                if (!string.IsNullOrWhiteSpace(root))
                {
                    ApiRoot = root;
                }
                else if (Config != null)
                {
                    ApiRoot = Config.Root!;
                }
            }

            if (Interceptor == null)
            {
                Type? handlerType = ClientAttribute.Handler;
                if (handlerType != null)
                {
                    object? handlerObj = EmitTool.CreateModel(handlerType);
                    if (handlerObj != null)
                    {
                        Interceptor = handlerObj as ISuperClientInterceptor;
                    }
                }
            }
        }

        public virtual SuperClientAttribute? ClientAttribute
        {
            get;
            set;
        }

        public virtual SuperClientConfig? Config
        {
            get;
            protected set;
        }

        public virtual string? ApiRoot
        {
            get;
            protected set;
        }

        public virtual ISuperClientInterceptor? Interceptor
        {
            get;
            set;
        }

        public virtual string GetApiUrl(string resource)
        {
            return PathTool.CombileUrl(ApiRoot!, resource);
        }

        public virtual Task<HttpResponseMessage?> RequestMessageAsync(string resource, SuperRequestOptions? options = null)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestMessageAsync(url, options);
        }

        public virtual Task<string?> RequestStringAsync(string resource, SuperRequestOptions? options = null)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestStringAsync(url, options);
        }

        public virtual Task<M?> RequestAsync<M>(string resource, SuperRequestOptions? options = null)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestAsync<M>(url, options);
        }

        public virtual Task<string?> RequestStringAsync<T>(string resource, HttpRequestOptions<T> options)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestStringAsync<T>(url, options);
        }

        public virtual Task<M?> RequestAsync<M, T>(string resource, HttpRequestOptions<T> options)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestAsync<M, T>(url, options);
        }

        public virtual Task<byte[]> RequestBufferAsync(string resource, SuperRequestOptions? options = null)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestBufferAsync(url, options);
        }

        public virtual Task<byte[]> RequestBufferAsync<T>(string resource, HttpRequestOptions<T> options)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestBufferAsync<T>(url, options);
        }

        public virtual Task<HttpResponseMessage> RequestMessageAsync(string resource, HttpRequestMultipartOptions? options = null)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestMessageAsync(url, options);
        }

        public virtual Task<string?> RequestStringAsync(string resource, HttpRequestMultipartOptions? options = null)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestStringAsync(url, options);
        }

        public virtual Task<M?> RequestAsync<M>(string resource, HttpRequestMultipartOptions? options = null)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestAsync<M>(url, options);
        }

        public virtual Task<string?> RequestStringAsync<T>(string resource, HttpRequestMultipartOptions<T>? options)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestStringAsync<T>(url, options);
        }

        public virtual Task<M?> RequestAsync<M, T>(string resource, HttpRequestMultipartOptions<T>? options)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestAsync<M, T>(url, options);
        }

        public virtual Task<byte[]?> RequestBufferAsync(string resource, HttpRequestMultipartOptions? options = null)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestBufferAsync(url, options);
        }

        public virtual Task<byte[]?> RequestBufferAsync<T>(string resource, HttpRequestMultipartOptions<T>? options)
        {
            HttpClient client = SuperClientUtility.GetClient(ApiRoot!);
            string url = GetApiUrl(resource);
            return client.RequestBufferAsync<T>(url, options);
        }

        public virtual async Task<T?> CallAsync<T>(Type apiType, string methodName, Dictionary<string, object?> arguments)
        {
            SuperClientAttribute clientAttr = ReflectionTool.GetCustomAttribute<SuperClientAttribute>(apiType)!;
            MethodInfo apiMethod = ReflectionTool.GetMethod(apiType, methodName)!;
            SuperRequestAttribute? apiAttr = apiMethod.GetCustomAttribute<SuperRequestAttribute>();
            ClientAttribute = clientAttr;
            TryLoadConfig();

            if (apiAttr != null && apiAttr is SuperPostAttribute)
            {
                return await CallAsync<T>(HttpMethod.Post, apiAttr, apiMethod, arguments).ConfigureAwait(false);
            }
            else if (apiAttr != null && apiAttr is SuperDeleteAttribute)
            {
                return await CallAsync<T>(HttpMethod.Delete, apiAttr, apiMethod, arguments).ConfigureAwait(false);
            }
            else if (apiAttr != null && apiAttr is SuperPutAttribute)
            {
                return await CallAsync<T>(HttpMethod.Put, apiAttr, apiMethod, arguments).ConfigureAwait(false);
            }
            else
            {
                return await CallAsync<T>(HttpMethod.Get, apiAttr, apiMethod, arguments).ConfigureAwait(false);
            }
        }


        private async Task<T?> CallAsync<T>(HttpMethod httpMethod, SuperRequestAttribute? apiAttr, MethodInfo apiMethod, Dictionary<string, object?> arguments)
        {
            T? result = default(T);
            Type type = typeof(T);
            TypeCode typeCode = Type.GetTypeCode(type);
            Type iebType = typeof(IEnumerable);

            string? resource = null;
            if (apiAttr != null)
            {
                resource = apiAttr.Resource;
            }
            if (string.IsNullOrEmpty(resource))
            {
                resource = apiMethod.Name;
            }

            ParameterInfo[] parameterInfos = apiMethod.GetParameters();

            SuperRequestOptions requestOptions = new SuperRequestOptions();
            requestOptions.Method = httpMethod;
            requestOptions.Handler = Interceptor;
            requestOptions.Config = Config;
            requestOptions.Headers = new Dictionary<string, string>();
            requestOptions.Cookies = new CookieCollection();
            requestOptions.InputData = new Dictionary<string, object?>();

            HttpRequestMultipartOptions requestFormOptions = new HttpRequestMultipartOptions();
            requestFormOptions.Method = httpMethod;
            requestFormOptions.Handler = Interceptor;
            requestFormOptions.Config = Config;
            requestFormOptions.Headers = new Dictionary<string, string>();
            requestFormOptions.Cookies = new CookieCollection();
            requestFormOptions.InputData = new Dictionary<string, object?>();
            requestFormOptions.UploadFiles = new Dictionary<string, MultipartFileInfo>();

            string handledResource = resource;
            bool isMultipartMode = false;
            bool isUseBody = false;

            Dictionary<string, RequestParamAttribute> paramAttrDict = new Dictionary<string, RequestParamAttribute>();
            Dictionary<string, RequestBodyAttribute> bodyAttrDict = new Dictionary<string, RequestBodyAttribute>();
            Dictionary<string, RequestHeaderAttribute> headerAttrDict = new Dictionary<string, RequestHeaderAttribute>();
            Dictionary<string, RequestCookieAttribute> cookieAttrDict = new Dictionary<string, RequestCookieAttribute>();
            Dictionary<string, RequestFormFileAttribute> formFileAttrDict = new Dictionary<string, RequestFormFileAttribute>();
            Dictionary<string, RequestPathParamAttribute> pathParamAttrDict = new Dictionary<string, RequestPathParamAttribute>();
            Dictionary<string, object> requestArgDict = new Dictionary<string, object>();
            if (parameterInfos.Any())
            {
                foreach (ParameterInfo paramInfo in parameterInfos)
                {
                    string paramName = paramInfo.Name!;

                    object? argValue = null;
                    if (!arguments.TryGetValue(paramName, out argValue))
                    {
                        continue;
                    }
                    if (argValue == null)
                    {
                        continue;
                    }
                    requestArgDict.Add(paramName, argValue);

                    RequestParamAttribute? paramItemAttr = ReflectionTool.GetCustomAttribute<RequestParamAttribute>(paramInfo);
                    RequestBodyAttribute? paramBodyAttr = ReflectionTool.GetCustomAttribute<RequestBodyAttribute>(paramInfo);
                    RequestHeaderAttribute? paramHeaderAttr = ReflectionTool.GetCustomAttribute<RequestHeaderAttribute>(paramInfo);
                    RequestCookieAttribute? paramCookieAttr = ReflectionTool.GetCustomAttribute<RequestCookieAttribute>(paramInfo);
                    RequestFormFileAttribute? paramFormFileAttr = ReflectionTool.GetCustomAttribute<RequestFormFileAttribute>(paramInfo);
                    RequestPathParamAttribute? pathParamAttr = ReflectionTool.GetCustomAttribute<RequestPathParamAttribute>(paramInfo);

                    bool hasAttr = false;
                    if (paramItemAttr != null)
                    {
                        paramAttrDict.Add(paramName, paramItemAttr);
                        hasAttr = true;
                    }
                    if (paramBodyAttr != null)
                    {
                        bodyAttrDict.Add(paramName, paramBodyAttr);
                        hasAttr = true;
                    }
                    if (paramHeaderAttr != null)
                    {
                        headerAttrDict.Add(paramName, paramHeaderAttr);
                        hasAttr = true;
                    }
                    if (paramCookieAttr != null)
                    {
                        cookieAttrDict.Add(paramName, paramCookieAttr);
                        hasAttr = true;
                    }
                    if (paramFormFileAttr != null)
                    {
                        formFileAttrDict.Add(paramName, paramFormFileAttr);
                        hasAttr = true;
                    }
                    if (pathParamAttr != null)
                    {
                        pathParamAttrDict.Add(paramName, pathParamAttr);
                        hasAttr = true;
                    }
                    if (!hasAttr)
                    {
                        paramAttrDict.Add(paramName, new RequestParamAttribute
                        {
                            Name = paramName
                        });
                    }
                }
            }

            if (formFileAttrDict.Count > 0 && httpMethod != HttpMethod.Post)
            {
                throw new SuperException(EX_KEY_RequestFormFileAttribute_Parameter_Not_Allowed);
            }
            if (formFileAttrDict.Count > 0 && bodyAttrDict.Count > 0)
            {
                throw new SuperException(EX_KEY_RequestBodyAttribute_Parameter_Not_Allowed);
            }

            if (pathParamAttrDict.Count > 0)
            {
                foreach (var kv in pathParamAttrDict)
                {
                    string paramName = kv.Key;
                    object argValue = requestArgDict[paramName];
                    RequestPathParamAttribute item = kv.Value;
                    if (!string.IsNullOrWhiteSpace(item.Name))
                    {
                        paramName = item.Name;
                    }
                    handledResource = handledResource.Replace("{" + paramName + "}", argValue.ToString()!);
                }
            }
            if ((paramAttrDict.Count > 0 && httpMethod != HttpMethod.Post) || (paramAttrDict.Count > 0 && bodyAttrDict.Count > 0))
            {
                foreach (var kv in paramAttrDict)
                {
                    string paramName = kv.Key;
                    object argValue = requestArgDict[paramName];
                    RequestParamAttribute item = kv.Value;
                    if (!string.IsNullOrWhiteSpace(item.Name))
                    {
                        paramName = item.Name;
                    }
                    Type argType = argValue.GetType();
                    TypeCode argTypeCode = Type.GetTypeCode(argType);

                    if (argType.IsValueType || argTypeCode == TypeCode.String || argType.IsEnum || argType == typeof(Guid))
                    {
                        handledResource = PathTool.SetUrlQuery(handledResource, paramName, argValue != null ? argValue.ToString()! : string.Empty, true);
                    }
                    else
                    {
                        if (iebType.IsAssignableFrom(argType))
                        {
                            IEnumerable ieb = (IEnumerable)argValue;
                            foreach (var e in ieb)
                            {
                                handledResource = PathTool.SetUrlQuery(handledResource, paramName, e != null ? e.ToString()! : string.Empty, true, false);
                            }
                        }
                        else
                        {
                            IDictionary<string, object?>? paramDict = GenericTool.ConvertToDictionary(argValue);
                            if (paramDict != null)
                            {
                                foreach (var paramKv in paramDict)
                                {
                                    handledResource = PathTool.SetUrlQuery(handledResource, paramKv.Key, paramKv.Value != null ? paramKv.Value.ToString()! : string.Empty, true);
                                }
                            }
                        }
                    }
                }
            }
            if (bodyAttrDict.Count > 0)
            {
                foreach (var kv in bodyAttrDict)
                {
                    string paramName = kv.Key;
                    object argValue = requestArgDict[paramName];
                    Type argType = argValue.GetType();
                    TypeCode argTypCode = Type.GetTypeCode(argType);

                    RequestBodyAttribute item = kv.Value;
                    if (!string.IsNullOrWhiteSpace(item.Name))
                    {
                        paramName = item.Name;
                    }

                    if (argType.IsValueType || argTypCode == TypeCode.String || argType.IsEnum || argType == typeof(Guid))
                    {
                        if (!requestOptions.InputData.ContainsKey(paramName))
                        {
                            requestOptions.InputData.Add(paramName, argValue);
                        }
                    }
                    else
                    {
                        IDictionary<string, object?>? bodyDict = GenericTool.ConvertToDictionary(argValue);
                        if (bodyDict != null)
                        {
                            foreach (var bodyKv in bodyDict)
                            {
                                if (!requestOptions.InputData.ContainsKey(bodyKv.Key))
                                {
                                    requestOptions.InputData.Add(bodyKv.Key, bodyKv.Value);
                                }
                            }
                        }
                    }
                }
                isUseBody = true;
            }
            if (formFileAttrDict.Count > 0)
            {
                foreach (var kv in formFileAttrDict)
                {
                    string paramName = kv.Key;
                    object argValue = requestArgDict[paramName];
                    
                    RequestFormFileAttribute item = kv.Value;
                    if (!string.IsNullOrWhiteSpace(item.Name))
                    {
                        paramName = item.Name;
                    }
                    byte[]? fileBuff = null;
                    if (argValue is byte[])
                    {
                        fileBuff = argValue as byte[];
                    }
                    else if (argValue is Stream)
                    {
                        Stream fileStream = (argValue as Stream)!;
                        fileBuff = new byte[fileStream.Length];
                        fileStream.Read(fileBuff);
                    }
                    else
                    {
                        throw new SuperException(EX_KEY_Invalid_RequestFormFileAttribute_Parameter_Value);
                    }
                    requestFormOptions.UploadFiles.Add(paramName, new MultipartFileInfo
                    {
                        Name = paramName,
                        FileName = item.FileName,
                        Data = fileBuff
                    });
                }
                isMultipartMode = true;
            }
            if (paramAttrDict.Count > 0 && httpMethod == HttpMethod.Post)
            {
                foreach (var kv in paramAttrDict)
                {
                    string paramName = kv.Key;
                    object argValue = requestArgDict[paramName];
                    Type argType = argValue.GetType();
                    TypeCode argTypCode = Type.GetTypeCode(argType);

                    RequestParamAttribute item = kv.Value;
                    if (!string.IsNullOrWhiteSpace(item.Name))
                    {
                        paramName = item.Name;
                    }

                    if (argType.IsValueType || argTypCode == TypeCode.String || argType.IsEnum || argType == typeof(Guid))
                    {
                        if (isMultipartMode)
                        {
                            requestFormOptions.InputData.Add(paramName, argValue);
                        }
                        else
                        {
                            requestOptions.InputData.Add(paramName, argValue);
                        }
                    }
                    else
                    {
                        IDictionary<string, object?>? paramDict = GenericTool.ConvertToDictionary(argValue);
                        if (paramDict != null)
                        {
                            foreach (var paramKv in paramDict)
                            {
                                if (isMultipartMode)
                                {
                                    requestFormOptions.InputData.Add(paramKv.Key, paramKv.Value != null ? paramKv.Value.ToString()! : string.Empty);
                                }
                                else
                                {
                                    requestOptions.InputData.Add(paramKv.Key, paramKv.Value != null ? paramKv.Value.ToString()! : string.Empty);
                                }
                            }
                        }
                    }
                }
            }
            if (headerAttrDict.Count > 0)
            {
                foreach (var kv in headerAttrDict)
                {
                    string paramName = kv.Key;
                    object argValue = requestArgDict[paramName];
                    RequestHeaderAttribute item = kv.Value;
                    if (!string.IsNullOrWhiteSpace(item.Name))
                    {
                        paramName = item.Name;
                    }
                    if (isMultipartMode)
                    {
                        requestFormOptions.Headers.Add(paramName, argValue.ToString()!);
                    }
                    else
                    {
                        requestOptions.Headers.Add(paramName, argValue.ToString()!);
                    }
                }
            }
            if (cookieAttrDict.Count > 0)
            {
                foreach (var kv in cookieAttrDict)
                {
                    string paramName = kv.Key;
                    object argValue = requestArgDict[paramName];
                    RequestCookieAttribute item = kv.Value;
                    if (!string.IsNullOrWhiteSpace(item.Name))
                    {
                        paramName = item.Name;
                    }
                    if (isMultipartMode)
                    {
                        requestFormOptions.Cookies.Add(new Cookie { Name = paramName, Value = argValue.ToString()! });
                    }
                    else
                    {
                        requestOptions.Cookies.Add(new Cookie { Name = paramName, Value = argValue.ToString()! });
                    }
                }
            }

            if (type.IsValueType || typeCode == TypeCode.String || type.IsEnum || type == typeof(Guid))
            {
                string? rspStr;
                if (isMultipartMode)
                {
                    rspStr = await RequestStringAsync(handledResource, requestFormOptions).ConfigureAwait(false);
                }
                else
                {
                    requestOptions.UseBody = isUseBody;
                    rspStr = await RequestStringAsync(handledResource, requestOptions).ConfigureAwait(false);
                }
                if (string.IsNullOrEmpty(rspStr))
                {
                    result = default(T);
                }
                else
                {
                    result = StringTool.TryParse<T>(rspStr);
                }
            }
            else
            {
                if (isMultipartMode)
                {
                    result = await RequestAsync<T>(handledResource, requestFormOptions).ConfigureAwait(false);
                }
                else
                {
                    requestOptions.UseBody = isUseBody;
                    result = await RequestAsync<T>(handledResource, requestOptions).ConfigureAwait(false);
                }
            }
            return result;
        }

    }
}
