using System;

namespace Rampage.DependencyInjection
{
    public interface IConditionalHandler
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="injectType"></param>
        /// <returns></returns>
        bool IsMatch(Type baseType, Type injectType);

    }
}
