using System;

namespace Rampage.Net
{
    /// <summary>
    /// 声明式 HTTP 请求标注基类：定义接口方法的资源路径与 HTTP 动词，由 SuperGet/SuperPost/SuperPut/SuperDelete 派生实现
    /// Base attribute for declarative HTTP requests: defines the interface method's resource path and HTTP verb; derived by SuperGet/SuperPost/SuperPut/SuperDelete
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public abstract class SuperRequestAttribute : Attribute
    {
        /// <summary>默认构造 / Default constructor</summary>
        public SuperRequestAttribute() { }

        /// <summary>以资源路径构造 / Constructs with a resource path</summary>
        /// <param name="resource">资源路径 / The resource path</param>
        public SuperRequestAttribute(string resource)
        {
            Resource = resource;
        }

        /// <summary>
        /// 资源路径：与 ApiRoot 拼接为完整请求地址；为空时默认取方法名；支持 {name} 占位符供 [RequestPathParam] 替换
        /// The resource path combined with ApiRoot into the full request URL; defaults to the method name when empty; supports {name} placeholders replaced by [RequestPathParam]
        /// </summary>
        public string? Resource
        {
            get; set;
        }

    }
}
