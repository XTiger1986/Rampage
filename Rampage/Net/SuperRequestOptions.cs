using Rampage.Reflection;
using System;
using System.Net;
using System.Reflection;

namespace Rampage.Net
{
    public class SuperRequestOptions
    {

        public SuperRequestOptions()
        {
            InputData = new Dictionary<string, object?>();
            Headers = new Dictionary<string, string>();
            Cookies = new CookieCollection();
        }

        public virtual HttpMethod Method { get; set; } = HttpMethod.Get;

        public ISuperClientInterceptor? Handler { get; set; }

        public SuperClientConfig? Config { get; set; }

        private IDictionary<string, object?>? _inputData;
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


        public IDictionary<string, string> Headers { get; set; }

        public CookieCollection Cookies { get; set; }

        public virtual bool UseBody { get; set; } = false;

        public object? InputObject { get; set; }

    }

    public class HttpRequestOptions<T> : SuperRequestOptions
    {

        private IDictionary<string, object?>? _inputData;
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

        public new T? InputObject { get; set; }

    }

    public class HttpRequestMultipartOptions : SuperRequestOptions
    {
        public override HttpMethod Method { get => HttpMethod.Post; }

        public virtual Dictionary<string, MultipartFileInfo>? UploadFiles { get; set; }

        public override bool UseBody { get => false; }

    }

    public class HttpRequestMultipartOptions<T> : HttpRequestMultipartOptions
    {

        private IDictionary<string, object?>? _inputData;
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

        public new T? InputObject { get; set; }

    }

}
