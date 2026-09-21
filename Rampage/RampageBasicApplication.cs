using Microsoft.Extensions.DependencyInjection;
using Rampage.DependencyInjection;
using System;

namespace Rampage
{
    /// <summary>
    /// 基础应用实现：创建全局作用域并对自身完成 [Auto] 注入 / Basic application implementation: creates the global scope and performs [Auto] injection on itself
    /// </summary>
    /// <remarks>
    /// Run 时创建名为 GLOBAL_SCOPE 的全局作用域，并在 RampageScope.UseGlobal 上下文中对应用实例自身执行 [Auto] 注入。
    /// Run creates the global scope named GLOBAL_SCOPE and performs [Auto] injection on the application instance itself inside RampageScope.UseGlobal.
    /// </remarks>
    public class RampageBasicApplication : SuperRampageApplication
    {

        /// <summary>
        /// 全局作用域（Run 时创建） / Global scope created during Run
        /// </summary>
        public virtual IServiceScope GlobalScope
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 创建全局作用域并对自身完成 [Auto] 属性注入 / Creates the global scope and performs [Auto] property injection on this instance
        /// </summary>
        /// <param name="args">启动参数 / Boot arguments</param>
        public override void Run(string[] args)
        {
            GlobalScope = CreateScope(Globals.GLOBAL_SCOPE_NAME);
            using (RampageScope.UseGlobal())
            {
                AutoInjecter autoInjecter = new AutoInjecter(RampageScope.RequireCurrent(nameof(RampageBasicApplication)).Locator);
                autoInjecter.Build(this);
            }
        }

        /// <summary>
        /// 释放基类资源与全局作用域 / Disposes base resources and the global scope
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            GlobalScope.Dispose();
        }

    }
}
