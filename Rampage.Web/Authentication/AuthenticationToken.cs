using System;

namespace Rampage.Web.Authentication
{
	/// <summary>
	/// 
	/// </summary>
	public class AuthenticationToken
	{

		/// <summary>
		/// 
		/// </summary>
		public string? Token
		{
			get;
			set;
		}

		/// <summary>
		/// 
		/// </summary>
		public string? RefreshToken
		{
			get;
			set;
		}

	}
}
