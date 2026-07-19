using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ActDim.AppRegistry.Domain.Core
{
    public class User : IEntity<Guid>
    {
        [Column("user_id")]
        public Guid Id { get; set; }

        public string Slug { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>
        /// EmailAddress
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Given name or first name of the user
        /// </summary>
        public string GivenName { get; set; }

        /// <summary>
        /// Surname or last name of the user
        /// </summary>
        public string FamilyName { get; set; }

        public string MiddleName { get; set; }

        public string PasswordHash { get; set; }

        public JsonDocument Metadata { get; set; }

        public string EntityTypeCode
        {
            get => Core.EntityTypeCode.User;
            set
            {
                if (value != Core.EntityTypeCode.User)
                    throw new ArgumentException($"Invalid value: {value}", nameof(value));
            }
        }

        public string Username
        {
            get => Slug;
            set
            {
                Slug = value;
            }
        }

        public string Name
        {
            get => Slug;
            set
            {
                Slug = value;
            }
        }

        // TODO:
        // Country
        // DateOfBirth
        // Gender
        // HomePhone
        // Locality
        // MobilePhone
        // OtherPhone
        // PostalCode
        // StateOrProvince
        // StreetAddress
    }
}
