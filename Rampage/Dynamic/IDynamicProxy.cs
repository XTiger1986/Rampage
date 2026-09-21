using System;

namespace Rampage.Dynamic
{
    /// <summary>
    /// 动态代理标记接口：由各 Dynamic*Builder 生成的代理/实现类型均实现此接口，便于运行时识别动态生成的类型
    /// Marker interface implemented by proxy/implementation types generated at runtime by the Dynamic*Builder classes
    /// </summary>
    public interface IDynamicProxy
    {
    }
}
