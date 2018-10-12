namespace YAF.Types.Objects
{
    using System.Runtime.Serialization;

    /// <summary>
    /// The Vokabular User
    /// </summary>
    [DataContract]
    public class VokabularUser
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        /// <value>
        /// The user ID.
        /// </value>
        [DataMember(Name = "sub")]
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>
        /// The email.
        /// </value>
        [DataMember(Name = "email")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        [DataMember(Name = "name")]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        /// <value>
        /// The first name.
        /// </value>
        [DataMember(Name = "given_name")]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        /// <value>
        /// The last name.
        /// </value>
        [DataMember(Name = "family_name")]
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the profile URL.
        /// </summary>
        /// <value>
        /// The profile URL.
        /// </value>
        [DataMember(Name = "link")]
        public string ProfileURL { get; set; }

        /// <summary>
        /// Gets or sets the profile image.
        /// </summary>
        /// <value>
        /// The profile image.
        /// </value>
        [DataMember(Name = "picture")]
        public string ProfileImage { get; set; }

        /// <summary>
        /// Gets or sets the gender.
        /// </summary>
        /// <value>
        /// The gender.
        /// </value>
        [DataMember(Name = "gender")]
        public string Gender { get; set; }

        /// <summary>
        /// Gets or sets the locale.
        /// </summary>
        /// <value>
        /// The locale.
        /// </value>
        [DataMember(Name = "locale")]
        public string Locale { get; set; }
    }
}