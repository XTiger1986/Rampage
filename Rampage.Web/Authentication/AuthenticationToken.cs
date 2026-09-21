using System;

namespace Rampage.Web.Authentication
{
	/// <summary>
	/// 认证令牌对：一次签发同时产出访问令牌与刷新令牌 / Authentication token pair: an access token and a refresh token issued together
	/// </summary>
	public class AuthenticationToken
	{

		/// <summary>
		/// 访问令牌（token_use=access），用于访问受保护资源 / Access token (token_use=access), used to access protected resources
		/// </summary>
		public string? Token
		{
			get;
			set;
		}

		/// <summary>
		/// 刷新令牌（token_use=refresh），用于在访问令牌过期后换取新的令牌对 / Refresh token (token_use=refresh), used to exchange for a new token pair after the access token expires
		/// </summary>
		public string? RefreshToken
		{
			get;
			set;
		}

	}
}
