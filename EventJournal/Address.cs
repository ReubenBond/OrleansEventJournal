// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Dapr Labs" file="Address.cs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace EventJournal
{
    using System;
    using System.Runtime.Serialization;

    using EventJournal.Utilities;

    using Newtonsoft.Json;

    /// <summary>
    /// Describes an address in the system.
    /// </summary>
    [Serializable]
    [DataContract]
    [JsonConverter(typeof(AddressJsonConverter))]
    public sealed class Address : IEquatable<Address>
    {
        /// <summary>
        /// The address split.
        /// </summary>
        private static readonly char[] AddressSplit = { '/' };

        /// <summary>
        /// Initializes a new instance of the <see cref="Address"/> class.
        /// </summary>
        public Address()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Address"/> class.
        /// </summary>
        /// <param name="kind">
        /// The type.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        public Address(string kind, Guid id)
            : this()
        {
            this.Kind = kind;
            this.Id = id;
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        [DataMember]
        public string Kind { get; set; }

        /// <summary>
        /// Convert the provided <paramref name="address"/> into an <see cref="Address"/> and return it.
        /// </summary>
        /// <param name="address">
        /// The address.
        /// </param>
        /// <returns>
        /// The <see cref="Address"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The provided <paramref name="address"/> is in the incorrect format.
        /// </exception>
        public static Address FromString(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            var split = address.Split(AddressSplit, 2, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
            {
                throw new ArgumentOutOfRangeException("address", "Address must be of the form \"{type:alphanum}/{id:hex}\", got: \"" + address + "\"");
            }

            var id = Guid.Parse(split[1]);

            return new Address { Id = id, Kind = split[0] };
        }

        /// <summary>
        /// Returns a value indicating whether or not the provided values are equal.
        /// </summary>
        /// <param name="left">
        /// The first value.
        /// </param>
        /// <param name="right">
        /// The second value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="left"/> is equal to <paramref name="right"/>, <see langword="false"/> otherwise.
        /// </returns>
        public static bool Equals(Address left, Address right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            return string.Equals(left.Kind, right.Kind, StringComparison.OrdinalIgnoreCase) && left.Id.Equals(right.Id);
        }

        /// <summary>
        /// Returns a value indicating whether or not the provided values are equal.
        /// </summary>
        /// <param name="left">The first value.</param>
        /// <param name="right">The second value.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="left"/> is equal to <paramref name="right"/>,
        /// <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator ==(Address left, Address right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Returns a value indicating whether or not the provided values are equal.
        /// </summary>
        /// <param name="left">The first value.</param>
        /// <param name="right">The second value.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="left"/> is equal to <paramref name="right"/>,
        /// <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator !=(Address left, Address right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Returns a value indicating whether or not the <paramref name="other"/> value is equal to this instance.
        /// </summary>
        /// <param name="other">
        /// The other value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="other"/> is equal to this instance, <see langword="false"/>
        /// otherwise.
        /// </returns>
        public bool Equals(Address other)
        {
            return Equals(this, other);
        }

        /// <summary>
        /// Returns a value indicating whether or not the <paramref name="obj"/> value is equal to this instance.
        /// </summary>
        /// <param name="obj">
        /// The other value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="obj"/> is equal to this instance, <see langword="false"/>
        /// otherwise.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(null, obj) || ReferenceEquals(null, this))
            {
                return false;
            }

            return obj is Address && this.Equals((Address)obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// The hash code for this instance.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.Kind != null ? this.Kind.ToLowerInvariant().GetHashCode() : 0) * 397) ^ this.Id.GetHashCode();
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return this.Kind + "/" + this.Id.ToString("N");
        }
    }
}
