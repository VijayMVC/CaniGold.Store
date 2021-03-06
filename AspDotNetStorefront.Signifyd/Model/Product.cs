// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
/* 
 * Signifyd API
 *
 * OpenAPI spec version: 1.0
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using SwaggerDateConverter = AspDotNetStorefront.Signifyd.Client.SwaggerDateConverter;

namespace AspDotNetStorefront.Signifyd.Model
{
    /// <summary>
    /// Product
    /// </summary>
    [DataContract]
    public partial class Product :  IEquatable<Product>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Product" /> class.
        /// </summary>
        /// <param name="ItemId">Your unique identifier for the product. This is a string because of hexadecimal identifiers..</param>
        /// <param name="ItemName">The name of the product..</param>
        /// <param name="ItemIsDigital">Indicates whether the item is electronically delivered e.g. gift cards.</param>
        /// <param name="ItemCategory">The name of the top-level product category. e.g. Apparel.</param>
        /// <param name="ItemSubCategory">The name of the sub-category of the product if applicable. e.g. T-Shirt.</param>
        /// <param name="ItemUrl">The url to the product&#39;s page..</param>
        /// <param name="ItemImage">The url to an image of the product..</param>
        /// <param name="ItemQuantity">The number of the items purchased..</param>
        /// <param name="ItemPrice">The price paid for each item (not the aggregate)..</param>
        /// <param name="ItemWeight">The weight of each item in grams..</param>
        public Product(string ItemId = default(string), string ItemName = default(string), bool? ItemIsDigital = default(bool?), string ItemCategory = default(string), string ItemSubCategory = default(string), string ItemUrl = default(string), string ItemImage = default(string), double? ItemQuantity = default(double?), double? ItemPrice = default(double?), double? ItemWeight = default(double?))
        {
            this.ItemId = ItemId;
            this.ItemName = ItemName;
            this.ItemIsDigital = ItemIsDigital;
            this.ItemCategory = ItemCategory;
            this.ItemSubCategory = ItemSubCategory;
            this.ItemUrl = ItemUrl;
            this.ItemImage = ItemImage;
            this.ItemQuantity = ItemQuantity;
            this.ItemPrice = ItemPrice;
            this.ItemWeight = ItemWeight;
        }
        
        /// <summary>
        /// Your unique identifier for the product. This is a string because of hexadecimal identifiers.
        /// </summary>
        /// <value>Your unique identifier for the product. This is a string because of hexadecimal identifiers.</value>
        [DataMember(Name="itemId", EmitDefaultValue=false)]
        public string ItemId { get; set; }

        /// <summary>
        /// The name of the product.
        /// </summary>
        /// <value>The name of the product.</value>
        [DataMember(Name="itemName", EmitDefaultValue=false)]
        public string ItemName { get; set; }

        /// <summary>
        /// Indicates whether the item is electronically delivered e.g. gift cards
        /// </summary>
        /// <value>Indicates whether the item is electronically delivered e.g. gift cards</value>
        [DataMember(Name="itemIsDigital", EmitDefaultValue=false)]
        public bool? ItemIsDigital { get; set; }

        /// <summary>
        /// The name of the top-level product category. e.g. Apparel
        /// </summary>
        /// <value>The name of the top-level product category. e.g. Apparel</value>
        [DataMember(Name="itemCategory", EmitDefaultValue=false)]
        public string ItemCategory { get; set; }

        /// <summary>
        /// The name of the sub-category of the product if applicable. e.g. T-Shirt
        /// </summary>
        /// <value>The name of the sub-category of the product if applicable. e.g. T-Shirt</value>
        [DataMember(Name="itemSubCategory", EmitDefaultValue=false)]
        public string ItemSubCategory { get; set; }

        /// <summary>
        /// The url to the product&#39;s page.
        /// </summary>
        /// <value>The url to the product&#39;s page.</value>
        [DataMember(Name="itemUrl", EmitDefaultValue=false)]
        public string ItemUrl { get; set; }

        /// <summary>
        /// The url to an image of the product.
        /// </summary>
        /// <value>The url to an image of the product.</value>
        [DataMember(Name="itemImage", EmitDefaultValue=false)]
        public string ItemImage { get; set; }

        /// <summary>
        /// The number of the items purchased.
        /// </summary>
        /// <value>The number of the items purchased.</value>
        [DataMember(Name="itemQuantity", EmitDefaultValue=false)]
        public double? ItemQuantity { get; set; }

        /// <summary>
        /// The price paid for each item (not the aggregate).
        /// </summary>
        /// <value>The price paid for each item (not the aggregate).</value>
        [DataMember(Name="itemPrice", EmitDefaultValue=false)]
        public double? ItemPrice { get; set; }

        /// <summary>
        /// The weight of each item in grams.
        /// </summary>
        /// <value>The weight of each item in grams.</value>
        [DataMember(Name="itemWeight", EmitDefaultValue=false)]
        public double? ItemWeight { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Product {\n");
            sb.Append("  ItemId: ").Append(ItemId).Append("\n");
            sb.Append("  ItemName: ").Append(ItemName).Append("\n");
            sb.Append("  ItemIsDigital: ").Append(ItemIsDigital).Append("\n");
            sb.Append("  ItemCategory: ").Append(ItemCategory).Append("\n");
            sb.Append("  ItemSubCategory: ").Append(ItemSubCategory).Append("\n");
            sb.Append("  ItemUrl: ").Append(ItemUrl).Append("\n");
            sb.Append("  ItemImage: ").Append(ItemImage).Append("\n");
            sb.Append("  ItemQuantity: ").Append(ItemQuantity).Append("\n");
            sb.Append("  ItemPrice: ").Append(ItemPrice).Append("\n");
            sb.Append("  ItemWeight: ").Append(ItemWeight).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            // credit: http://stackoverflow.com/a/10454552/677735
            return this.Equals(obj as Product);
        }

        /// <summary>
        /// Returns true if Product instances are equal
        /// </summary>
        /// <param name="other">Instance of Product to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Product other)
        {
            // credit: http://stackoverflow.com/a/10454552/677735
            if (other == null)
                return false;

            return 
                (
                    this.ItemId == other.ItemId ||
                    this.ItemId != null &&
                    this.ItemId.Equals(other.ItemId)
                ) && 
                (
                    this.ItemName == other.ItemName ||
                    this.ItemName != null &&
                    this.ItemName.Equals(other.ItemName)
                ) && 
                (
                    this.ItemIsDigital == other.ItemIsDigital ||
                    this.ItemIsDigital != null &&
                    this.ItemIsDigital.Equals(other.ItemIsDigital)
                ) && 
                (
                    this.ItemCategory == other.ItemCategory ||
                    this.ItemCategory != null &&
                    this.ItemCategory.Equals(other.ItemCategory)
                ) && 
                (
                    this.ItemSubCategory == other.ItemSubCategory ||
                    this.ItemSubCategory != null &&
                    this.ItemSubCategory.Equals(other.ItemSubCategory)
                ) && 
                (
                    this.ItemUrl == other.ItemUrl ||
                    this.ItemUrl != null &&
                    this.ItemUrl.Equals(other.ItemUrl)
                ) && 
                (
                    this.ItemImage == other.ItemImage ||
                    this.ItemImage != null &&
                    this.ItemImage.Equals(other.ItemImage)
                ) && 
                (
                    this.ItemQuantity == other.ItemQuantity ||
                    this.ItemQuantity != null &&
                    this.ItemQuantity.Equals(other.ItemQuantity)
                ) && 
                (
                    this.ItemPrice == other.ItemPrice ||
                    this.ItemPrice != null &&
                    this.ItemPrice.Equals(other.ItemPrice)
                ) && 
                (
                    this.ItemWeight == other.ItemWeight ||
                    this.ItemWeight != null &&
                    this.ItemWeight.Equals(other.ItemWeight)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            // credit: http://stackoverflow.com/a/263416/677735
            unchecked // Overflow is fine, just wrap
            {
                int hash = 41;
                // Suitable nullity checks etc, of course :)
                if (this.ItemId != null)
                    hash = hash * 59 + this.ItemId.GetHashCode();
                if (this.ItemName != null)
                    hash = hash * 59 + this.ItemName.GetHashCode();
                if (this.ItemIsDigital != null)
                    hash = hash * 59 + this.ItemIsDigital.GetHashCode();
                if (this.ItemCategory != null)
                    hash = hash * 59 + this.ItemCategory.GetHashCode();
                if (this.ItemSubCategory != null)
                    hash = hash * 59 + this.ItemSubCategory.GetHashCode();
                if (this.ItemUrl != null)
                    hash = hash * 59 + this.ItemUrl.GetHashCode();
                if (this.ItemImage != null)
                    hash = hash * 59 + this.ItemImage.GetHashCode();
                if (this.ItemQuantity != null)
                    hash = hash * 59 + this.ItemQuantity.GetHashCode();
                if (this.ItemPrice != null)
                    hash = hash * 59 + this.ItemPrice.GetHashCode();
                if (this.ItemWeight != null)
                    hash = hash * 59 + this.ItemWeight.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

}
