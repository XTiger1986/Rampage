using Microsoft.Extensions.DependencyInjection;
using Rampage.DependencyInjection;
using System;

namespace Rampage
{
    /// <summary>
    /// 基础的程序启动器
    /// </summary>
    public class RampageBasicApplication : SuperRampageApplication
    {

        public virtual IServiceScope GlobalScope
        {
            get;
            private set;
        } = default!;

        public override void Run(string[] args)
        {
            GlobalScope = CreateScope(Globals.GLOBAL_SCOPE_NAME);
            using (RampageScope.UseGlobal())
            {
                AutoInjecter autoInjecter = new AutoInjecter(RampageScope.RequireCurrent(nameof(RampageBasicApplication)).Locator);
                autoInjecter.Build(this);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            GlobalScope.Dispose();
        }

    }
}
