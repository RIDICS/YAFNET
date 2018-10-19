using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using YAF.Classes;
using YAF.Classes.Data;
using YAF.Core.Model;
using YAF.Types.Constants;
using YAF.Types.EventProxies;
using YAF.Types.Extensions;
using YAF.Types.Interfaces;
using YAF.Types.Models;
using YAF.Types.Objects;
using YAF.Utils;
using YAF.Utils.Extensions;
using YAF.Utils.Helpers;

namespace YAF.Core.Services.Auth
{
    /// <summary>
    /// Vokabular Single Sign On Class
    /// </summary>
    public class Vokabular : IAuthBase
    {
        /// <summary>
        ///   Gets or sets the User IP Info.
        /// </summary>
        private IDictionary<string, string> UserIpLocator { get; set; }

        private const string Issuer = "http://localhost:5000";
        private const string AuthorizationEndpoint = Issuer + "/connect/authorize";
        private const string TokenEndpoint = Issuer + "/connect/token";
        private const string UserInfoEndpoint = Issuer + "/connect/userinfo?alt=json";
        private const string IntrospectEndpoint = Issuer + "/connect/introspect";
        private const string Scopes = "openid profile email address";
    
        /// <summary>
        /// Gets the authorize URL.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <returns>
        /// Returns the Authorize URL
        /// </returns>
        public string GetAuthorizeUrl(HttpRequest request)
        {
            return (AuthorizationEndpoint + "?client_id={0}&response_type={1}&scope={2}&redirect_uri={3}").FormatWith
                (Config.VokabularClientID,
                "code",
                HttpUtility.UrlEncode(Scopes),
                HttpUtility.UrlEncode(GetRedirectURL(request)));
        }

        /// <summary>
        /// Gets the access token.
        /// </summary>
        /// <param name="authorizationCode">
        /// The authorization code.
        /// </param>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <returns>
        /// Returns the Access Token
        /// </returns>
        public VokabularTokens GetAccessToken(string authorizationCode, HttpRequest request)
        {
            var code = "code={0}".FormatWith(HttpUtility.UrlEncode(authorizationCode)); 
            var data = "client_id={1}&client_secret={2}&grant_type={4}&{0}&redirect_uri={3}".FormatWith(
                code,
                Config.VokabularClientID,
                Config.VokabularClientSecret,
                HttpUtility.UrlEncode(GenerateLoginUrl(false)),
                "authorization_code");

            var response = AuthUtilities.WebRequest(
                AuthUtilities.Method.POST,
                TokenEndpoint,
                data);

            return response.FromJson<VokabularTokens>();
        }

        #region Get Current Vokabular User Profile

        /// <summary>
        /// Get The Vokabular User Profile of the Current User
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <param name="accessToken">
        /// The access_token.
        /// </param>
        /// <returns>
        /// Returns the VokabularUser Profile
        /// </returns>
        public VokabularUser GetVokabularUser(HttpRequest request, string accessToken)
        {
            var headers = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(
                    "Authorization",
                    "Bearer {0}".FormatWith(accessToken))
            };

            var response = AuthUtilities.WebRequest(
                AuthUtilities.Method.GET,
                UserInfoEndpoint,
                string.Empty,
                headers);
            return response.FromJson<VokabularUser>();
        }

        
        #endregion

        /// <summary>
        /// Generates the login URL.
        /// </summary>
        /// <param name="generatePopUpUrl">
        /// if set to <c>true</c> [generate pop up URL].
        /// </param>
        /// <param name="connectCurrentUser">
        /// if set to <c>true</c> [connect current user].
        /// </param>
        /// <returns>
        /// Returns the login URL
        /// </returns>
        public string GenerateLoginUrl(bool generatePopUpUrl, bool connectCurrentUser = false)
        {
            var authUrl = "{0}auth.aspx?auth={1}{2}".FormatWith(
                YafForumInfo.ForumBaseUrl,
                AuthService.vokabular,
                connectCurrentUser ? "&connectCurrent=true" : string.Empty);

            return authUrl;
        }

        public bool LoginOrCreateUser(HttpRequest request, string parameters, out string message)
        {
            throw new NotImplementedException(); //TODO
        }

        public bool ValidateAccessToken(string accessToken)
        {
            var headers = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(
                    "Authorization",
                    "Basic {0}".FormatWith(Convert.ToBase64String(Encoding.UTF8.GetBytes(
                        $"{Config.VokabularClientID}:{Config.VokabularClientSecret}"))))
            };

            var data = "token={0}&token_type_hint=access_token".FormatWith(accessToken);

            var response = AuthUtilities.WebRequest(
                AuthUtilities.Method.POST,
                IntrospectEndpoint,
                data,
                headers
            );

            return response.FromJson<VokabularValidationResponse>().Active;
        }

        /// <summary>
        /// Logins the or create user.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <param name="tokens">
        /// The access and ID token.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// Returns if Login was successful or not
        /// </returns>
        public bool LoginOrCreateUser(HttpRequest request, VokabularTokens tokens, out string message)
        {
            if (!YafContext.Current.Get<YafBoardSettings>().AllowSingleSignOn)
            {
                message = YafContext.Current.Get<ILocalization>().GetText("LOGIN", "SSO_DEACTIVATED");

                return false;
            }

            var vokabularUser = this.GetVokabularUser(request, tokens.AccessToken);

            var userGender = 0;

            if (vokabularUser.Gender.IsSet())
            {
                switch (vokabularUser.Gender)
                {
                    case "male":
                        userGender = 1;
                        break;
                    case "female":
                        userGender = 2;
                        break;
                }
            }

            // Check if user exists
            var userName = YafContext.Current.Get<MembershipProvider>().GetUserNameByEmail(vokabularUser.Email);

            if (userName.IsNotSet())
            {
                // Create User if not exists?!
                return this.CreateVokabularUser(vokabularUser, userGender, out message);
            }

            var yafUserData =
                new CombinedUserDataHelper(YafContext.Current.Get<MembershipProvider>().GetUser(userName, true));

            var handler = new JwtSecurityTokenHandler();
            var token = (JwtSecurityToken) handler.ReadToken(tokens.IDToken);
            if (token.Subject == vokabularUser.Subject 
                && token.Audiences.Contains(Config.VokabularClientID) 
                && token.Audiences.Count() == 1 
                && token.Issuer == Issuer)//&& ValidateAccessToken(tokens.AccessToken))
            {
                YafSingleSignOnUser.LoginSuccess(AuthService.vokabular, userName, yafUserData.UserID, true);

                message = string.Empty;
                return true;
            }

            message = YafContext.Current.Get<ILocalization>().GetText("LOGIN", "SSO_VOKABULAR_FAILED2");
            return false;
        }

        /// <summary>
        /// Connects the user.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <param name="parameters">
        /// The access token.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// Returns if the connect was successful or not
        /// </returns>
        public bool ConnectUser(HttpRequest request, string parameters, out string message)
        {
            var vokabularUser = this.GetVokabularUser(request, parameters);

            var userGender = 0;

            if (vokabularUser.Gender.IsSet())
            {
                switch (vokabularUser.Gender)
                {
                    case "male":
                        userGender = 1;
                        break;
                    case "female":
                        userGender = 2;
                        break;
                }
            }

            // Create User if not exists?!
            if (!YafContext.Current.IsGuest)
            {
                // Match the Email address?
                if (vokabularUser.Email != YafContext.Current.CurrentUserData.Email)
                {
                    message = YafContext.Current.Get<ILocalization>().GetText("LOGIN", "SSO_VOKABULARNAME_NOTMATCH");

                    return false;
                }

                // Update profile with Vokabular informations
                var userProfile = YafContext.Current.Profile;

                userProfile.Gender = userGender;

                userProfile.Save();

                // save avatar
                LegacyDb.user_saveavatar(YafContext.Current.PageUserID, vokabularUser.ProfileImage, null, null);

                YafSingleSignOnUser.LoginSuccess(AuthService.vokabular, null, YafContext.Current.PageUserID, false);

                message = string.Empty;

                return true;
            }

            message = YafContext.Current.Get<ILocalization>().GetText("LOGIN", "SSO_VOKABULAR_FAILED");
            return false;
        }

        /// <summary>
        /// Gets the redirect URL.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <returns>
        /// Returns the Redirect URL
        /// </returns>
        private static string GetRedirectURL(HttpRequest request)
        {
            var urlCurrentPage = request.Url.AbsoluteUri.IndexOf('?') == -1
                ? request.Url.AbsoluteUri
                : request.Url.AbsoluteUri.Substring(0, request.Url.AbsoluteUri.IndexOf('?'));

            var nvc = new NameValueCollection();

            foreach (var key in request.QueryString.Cast<string>().Where(key => key != "code"))
            {
                nvc.Add(key, request.QueryString[key]);
            }

            var queryString = string.Empty;

            foreach (string key in nvc)
            {
                queryString += queryString == string.Empty ? "?" : "&";
                queryString += "{0}={1}".FormatWith(key, nvc[key]);
            }

            return "{0}{1}".FormatWith(urlCurrentPage, queryString);
        }

        /// <summary>
        /// Creates the Vokabular user
        /// </summary>
        /// <param name="vokabularUser">
        /// The Vokabular user.
        /// </param>
        /// <param name="userGender">
        /// The user gender.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// Returns if the login was successfully or not
        /// </returns>
        private bool CreateVokabularUser(VokabularUser vokabularUser, int userGender, out string message)
        {
            // Check user for bot
            var spamChecker = new YafSpamCheck();
            string result;
            var isPossibleSpamBot = false;

            var userIpAddress = YafContext.Current.Get<HttpRequestBase>().GetUserRealIPAddress();

            // Check content for spam
            if (spamChecker.CheckUserForSpamBot(vokabularUser.UserName, vokabularUser.Email, userIpAddress, out result))
            {
                YafContext.Current.Get<ILogger>().Log(
                    null,
                    "Bot Detected",
                    "Bot Check detected a possible SPAM BOT: (user name : '{0}', email : '{1}', ip: '{2}', reason : {3}), user was rejected."
                        .FormatWith(vokabularUser.UserName, vokabularUser.Email, userIpAddress, result),
                    EventLogTypes.SpamBotDetected);

                if (YafContext.Current.Get<YafBoardSettings>().BotHandlingOnRegister.Equals(1))
                {
                    // Flag user as spam bot
                    isPossibleSpamBot = true;
                }
                else if (YafContext.Current.Get<YafBoardSettings>().BotHandlingOnRegister.Equals(2))
                {
                    message = YafContext.Current.Get<ILocalization>().GetText("BOT_MESSAGE");

                    if (!YafContext.Current.Get<YafBoardSettings>().BanBotIpOnDetection)
                    {
                        return false;
                    }

                    YafContext.Current.GetRepository<BannedIP>()
                        .Save(
                            null,
                            userIpAddress,
                            "A spam Bot who was trying to register was banned by IP {0}".FormatWith(userIpAddress),
                            YafContext.Current.PageUserID);

                    // Clear cache
                    YafContext.Current.Get<IDataCache>().Remove(Constants.Cache.BannedIP);

                    if (YafContext.Current.Get<YafBoardSettings>().LogBannedIP)
                    {
                        YafContext.Current.Get<ILogger>()
                            .Log(
                                null,
                                "IP BAN of Bot During Registration",
                                "A spam Bot who was trying to register was banned by IP {0}".FormatWith(
                                    userIpAddress),
                                EventLogTypes.IpBanSet);
                    }

                    return false;
                }
            }

            MembershipCreateStatus status;

            var memberShipProvider = YafContext.Current.Get<MembershipProvider>();

            var pass = Membership.GeneratePassword(32, 16);
            var securityAnswer = Membership.GeneratePassword(64, 30);

            var user = memberShipProvider.CreateUser(
                vokabularUser.UserName,
                pass,
                vokabularUser.Email,
                memberShipProvider.RequiresQuestionAndAnswer ? "Answer is a generated Pass" : null,
                memberShipProvider.RequiresQuestionAndAnswer ? securityAnswer : null,
                true,
                null,
                out status);

            // setup initial roles (if any) for this user
            RoleMembershipHelper.SetupUserRoles(YafContext.Current.PageBoardID, vokabularUser.UserName);

            // create the user in the YAF DB as well as sync roles...
            var userID = RoleMembershipHelper.CreateForumUser(user, YafContext.Current.PageBoardID);

            // create empty profile just so they have one
            var userProfile = YafUserProfile.GetProfile(vokabularUser.UserName);

            // setup their initial profile information
            userProfile.Save();

            userProfile.Gender = userGender;
            
            if (YafContext.Current.Get<YafBoardSettings>().EnableIPInfoService && this.UserIpLocator == null)
            {
                this.UserIpLocator = new IPDetails().GetData(
                    YafContext.Current.Get<HttpRequestBase>().GetUserRealIPAddress(),
                    "text",
                    false,
                    YafContext.Current.CurrentForumPage.Localization.Culture.Name,
                    string.Empty,
                    string.Empty);

                if (this.UserIpLocator != null && this.UserIpLocator["StatusCode"] == "OK"
                                               && this.UserIpLocator.Count > 0)
                {
                    userProfile.Country = this.UserIpLocator["CountryCode"];

                    var location = new StringBuilder();

                    if (this.UserIpLocator["RegionName"] != null && this.UserIpLocator["RegionName"].IsSet()
                                                                 && !this.UserIpLocator["RegionName"].Equals("-"))
                    {
                        location.Append(this.UserIpLocator["RegionName"]);
                    }

                    if (this.UserIpLocator["CityName"] != null && this.UserIpLocator["CityName"].IsSet()
                                                               && !this.UserIpLocator["CityName"].Equals("-"))
                    {
                        location.AppendFormat(", {0}", this.UserIpLocator["CityName"]);
                    }

                    userProfile.Location = location.ToString();
                }
            }

            userProfile.Save();

            if (userID == null)
            {
                // something is seriously wrong here -- redirect to failure...
                message = YafContext.Current.Get<ILocalization>().GetText("LOGIN", "SSO_FAILED");
                return false;
            }

            if (YafContext.Current.Get<YafBoardSettings>().NotificationOnUserRegisterEmailList.IsSet())
            {
                // send user register notification to the following admin users...
                YafContext.Current.Get<ISendNotification>().SendRegistrationNotificationEmail(user, userID.Value);
            }

            if (isPossibleSpamBot)
            {
                YafContext.Current.Get<ISendNotification>().SendSpamBotNotificationToAdmins(user, userID.Value);
            }

            // send user register notification to the user...
            YafContext.Current.Get<ISendNotification>()
                .SendRegistrationNotificationToUser(user, pass, securityAnswer,
                    "NOTIFICATION_ON_VOKABULAR_REGISTER");

            // save the time zone...
            var userId = UserMembershipHelper.GetUserIDFromProviderUserKey(user.ProviderUserKey);

            var autoWatchTopicsEnabled = YafContext.Current.Get<YafBoardSettings>().DefaultNotificationSetting
                                         == UserNotificationSetting.TopicsIPostToOrSubscribeTo;

            LegacyDb.user_save(
                userID: userId,
                boardID: YafContext.Current.PageBoardID,
                userName: vokabularUser.UserName,
                displayName: vokabularUser.UserName,
                email: vokabularUser.Email,
                timeZone: TimeZoneInfo.Local.Id,
                languageFile: null,
                culture: null,
                themeFile: null,
                textEditor: null,
                useMobileTheme: null,
                approved: null,
                pmNotification: YafContext.Current.Get<YafBoardSettings>().DefaultNotificationSetting,
                autoWatchTopics: autoWatchTopicsEnabled,
                dSTUser: TimeZoneInfo.Local.SupportsDaylightSavingTime,
                hideUser: null,
                notificationType: null);

            // save the settings...
            LegacyDb.user_savenotification(
                userId,
                true,
                autoWatchTopicsEnabled,
                YafContext.Current.Get<YafBoardSettings>().DefaultNotificationSetting,
                YafContext.Current.Get<YafBoardSettings>().DefaultSendDigestEmail);

            // save avatar
            //LegacyDb.user_saveavatar(userId, vokabularUser.ProfileImage, null, null);

            YafContext.Current.Get<IRaiseEvent>().Raise(new NewUserRegisteredEvent(user, userId));

            YafSingleSignOnUser.LoginSuccess(AuthService.vokabular, user.UserName, userId, true);

            message = string.Empty;

            return true;
        }
    }
}