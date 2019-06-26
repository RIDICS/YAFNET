namespace YAF.Types.Objects
{
    using System.Runtime.Serialization;

    /// <summary>
    /// The Vokabular User
    /// </summary>
    [DataContract]
    public class VokabularUser
    {
        [DataMember(Name = "sub")]
        public string Subject { get; set; }

        [DataMember(Name = "email")]
        public string Email { get; set; }

        [DataMember(Name = "name")]
        public string UserName { get; set; }

        [DataMember(Name = "given_name")]
        public string FirstName { get; set; }

        [DataMember(Name = "family_name")]
        public string LastName { get; set; }

        [DataMember(Name = "phone_number")]
        public string PhoneNumber { get; set; }

        [DataMember(Name = "picture")]
        public string ProfileImage { get; set; }
        
        [DataMember(Name = "gender")]
        public string Gender { get; set; }

        [DataMember(Name = "locale")]
        public string Locale { get; set; }
    }
}