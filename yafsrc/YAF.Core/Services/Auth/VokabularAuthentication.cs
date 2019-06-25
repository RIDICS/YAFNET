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
    public class VokabularAuthentication : IAuthBase
    {
        /// <summary>
        ///   Gets or sets the User IP Info.
        /// </summary>
        private IDictionary<string, string> UserIpLocator { get; set; }

        private readonly string m_authorizationEndpoint = Config.OidcUrl + "/connect/authorize";
        private readonly string m_tokenEndpoint = Config.OidcUrl + "/connect/token";
        private readonly string m_userInfoEndpoint = Config.OidcUrl + "/connect/userinfo?alt=json";
        private readonly string m_introspectEndpoint = Config.OidcUrl + "/connect/introspect";
        private readonly string m_scopes = "openid profile";

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
            return (m_authorizationEndpoint + "?client_id={0}&response_type={1}&scope={2}&redirect_uri={3}").FormatWith
                (Config.OidcClientId,
                "code",
                HttpUtility.UrlEncode(m_scopes),
                HttpUtility.UrlEncode(GetRedirectUrl(request)));
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
                Config.OidcClientId,
                Config.OidcClientSecret,
                HttpUtility.UrlEncode(GenerateLoginUrl(false)),
                "authorization_code");

            var response = AuthUtilities.WebRequest(
                AuthUtilities.Method.POST,
                m_tokenEndpoint,
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
                m_userInfoEndpoint,
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

        private bool ValidateAccessToken(string accessToken)
        {
            var headers = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(
                    "Authorization",
                    "Basic {0}".FormatWith(Convert.ToBase64String(Encoding.UTF8.GetBytes(
                        $"{Config.OidcClientId}:{Config.OidcClientSecret}"))))
            };

            var data = "token={0}".FormatWith(accessToken);

            var response = AuthUtilities.WebRequest(
                AuthUtilities.Method.POST,
                m_introspectEndpoint,
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

            var vokabularUser = GetVokabularUser(request, tokens.AccessToken);

            var userGender = 0; //male = 1, female = 2

            // Check if user exists
            var membershipUser = YafContext.Current.Get<MembershipProvider>().GetUser(vokabularUser.UserName, true);

            if (membershipUser == null)
            {
                // Create User if not exists?!
                return CreateVokabularUser(vokabularUser, userGender, out message);
            }
            else
            {
                UpdateVokabularUser(vokabularUser, membershipUser, out message);
            }

            
            var yafUserData =
                new CombinedUserDataHelper(YafContext.Current.Get<MembershipProvider>().GetUser(membershipUser.UserName, true));

            var yafUser = YafUserProfile.GetProfile(membershipUser.UserName);

            var handler = new JwtSecurityTokenHandler();
            var token = (JwtSecurityToken) handler.ReadToken(tokens.IdToken);

            if (token.Subject == vokabularUser.Subject
                && token.Audiences.Contains(Config.OidcClientId)
                && token.Audiences.Count() == 1
                && token.Issuer == Config.OidcUrl
                && token.ValidFrom < DateTime.UtcNow
                && token.ValidTo > DateTime.UtcNow
                && yafUser.VokabularId == token.Subject)
            {
                YafSingleSignOnUser.LoginSuccess(AuthService.vokabular, membershipUser.UserName, yafUserData.UserID, true);

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
            var vokabularUser = GetVokabularUser(request, parameters);

            var userGender = 0;

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
        private static string GetRedirectUrl(HttpRequest request)
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
            var isPossibleSpamBot = false;

            var userIpAddress = YafContext.Current.Get<HttpRequestBase>().GetUserRealIPAddress();

            // Check content for spam
            string result;
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

            var memberShipProvider = YafContext.Current.Get<MembershipProvider>();

            var pass = Membership.GeneratePassword(32, 16);
            var securityAnswer = Membership.GeneratePassword(64, 30);

            MembershipCreateStatus status;
            var user = memberShipProvider.CreateUser(
                vokabularUser.UserName,
                pass,
                vokabularUser.Email,
                memberShipProvider.RequiresQuestionAndAnswer
                    ? YafContext.Current.Get<ILocalization>().GetText("LOGIN", "GENERATED_QUESTION")
                    : null,
                memberShipProvider.RequiresQuestionAndAnswer ? securityAnswer : null,
                true,
                null,
                out status);

            if (status != MembershipCreateStatus.Success)
            {
                throw new InvalidOperationException("An error occurred while creating a new user: " + status);
            }
            
            // setup initial roles (if any) for this user
            RoleMembershipHelper.SetupUserRoles(YafContext.Current.PageBoardID, vokabularUser.UserName);

            // create the user in the YAF DB as well as sync roles...
            var userID = RoleMembershipHelper.CreateForumUser(user, YafContext.Current.PageBoardID);

            // create empty profile just so they have one
            var userProfile = YafUserProfile.GetProfile(vokabularUser.UserName);

            // setup their initial profile information
            userProfile.Save();

            userProfile.Gender = userGender;
            userProfile.RealName = $"{vokabularUser.FirstName} {vokabularUser.LastName}";
            userProfile.VokabularId = vokabularUser.Subject;

            if (YafContext.Current.Get<YafBoardSettings>().EnableIPInfoService && UserIpLocator == null)
            {
                UserIpLocator = new IPDetails().GetData(
                    YafContext.Current.Get<HttpRequestBase>().GetUserRealIPAddress(),
                    "text",
                    false,
                    YafContext.Current.CurrentForumPage.Localization.Culture.Name,
                    string.Empty,
                    string.Empty);

                if (UserIpLocator != null && UserIpLocator["StatusCode"] == "OK"
                                               && UserIpLocator.Count > 0)
                {
                    userProfile.Country = UserIpLocator["CountryCode"];

                    var location = new StringBuilder();

                    if (UserIpLocator["RegionName"] != null && UserIpLocator["RegionName"].IsSet()
                                                                 && !UserIpLocator["RegionName"].Equals("-"))
                    {
                        location.Append(UserIpLocator["RegionName"]);
                    }

                    if (UserIpLocator["CityName"] != null && UserIpLocator["CityName"].IsSet()
                                                               && !UserIpLocator["CityName"].Equals("-"))
                    {
                        location.AppendFormat(", {0}", UserIpLocator["CityName"]);
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

        private bool UpdateVokabularUser(VokabularUser vokabularUser, MembershipUser membershipUser, out string message)
        {
            var memberShipProvider = YafContext.Current.Get<MembershipProvider>();

            membershipUser.Email = vokabularUser.Email;
            memberShipProvider.UpdateUser(membershipUser);
  
 
            // create empty profile just so they have one
            var userProfile = YafUserProfile.GetProfile(vokabularUser.UserName);

            userProfile.VokabularId = vokabularUser.Subject;
            userProfile.RealName = $"{vokabularUser.FirstName} {vokabularUser.LastName}";
            userProfile.Save();

            // save the time zone...
            var userId = UserMembershipHelper.GetUserIDFromProviderUserKey(membershipUser.ProviderUserKey);

            var userData = new CombinedUserDataHelper(membershipUser);
            var test = userData.DBRow;
            LegacyDb.user_save(
                userID: userId,
                boardID: YafContext.Current.PageBoardID,
                userName: vokabularUser.UserName,
                displayName: vokabularUser.UserName,
                email: vokabularUser.Email,
                timeZone: userData.TimeZone,
                languageFile: userData.LanguageFile,
                culture: userData.CultureUser,
                themeFile: userData.ThemeFile,
                textEditor: userData.TextEditor,
                useMobileTheme: userData.UseMobileTheme,
                approved: null,
                pmNotification: userData.PMNotification,
                autoWatchTopics: userData.AutoWatchTopics,
                dSTUser: userData.DSTUser,
                hideUser: null,
                notificationType: null);

            // save avatar
            //LegacyDb.user_saveavatar(userId, vokabularUser.ProfileImage, null, null);
            message = string.Empty;

            return true;
        }
    }
}