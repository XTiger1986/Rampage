using System;
namespace Rampage
{
    public class RampageApplicationContext : IRampageApplicationContext
    {

        public RampageApplicationContext(IRampageApplication app)
        {
            Application = app;
        }

        public IRampageApplication Application { get; private set; }

    }
}
