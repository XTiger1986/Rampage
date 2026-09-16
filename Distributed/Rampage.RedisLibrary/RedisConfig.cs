using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// Redis Configuration
    /// </summary>
    public class RedisConfig
    {

        public string Name
        {
            get;
            set;
        } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        public string ConnectionString
        {
            get;
            set;
        } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        public string PrefixKey
        {
            get;
            set;
        } = string.Empty;

        private int _db = -1;
        /// <summary>
        /// 
        /// </summary>
        public int Db
        {
            get { return _db; }
            set { _db = value; }
        }

    }
}
