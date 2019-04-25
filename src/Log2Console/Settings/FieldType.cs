using System.ComponentModel;
using Log2Console.Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Log2Console.Settings
{
    public class FieldType
    {
        protected FieldType()
        {
        }

        public FieldType(LogMessageField field, string name, string property = null)
        {
            Field = field;
            Name = name;
            Property = property;
        }

        /// <summary>
        ///     Gets or sets the type of field.
        /// </summary>
        /// <value>
        ///     The field.
        /// </value>
        [Category("Field Configuration")]
        [DisplayName("Field Type")]
        [Description("The Type of the Field")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LogMessageField Field { get; set; }

        /// <summary>
        ///     If the Field is of type Property, specify the name of the Property
        /// </summary>
        /// <value>
        ///     The property.
        /// </value>
        [Category("Field Configuration")]
        [DisplayName("Property")]
        [Description("The Name of the Property")]
        public string Property { get; set; }

        /// <summary>
        ///     The Display / Column name of the Field
        /// </summary>
        /// <value>
        ///     The name of the field.
        /// </value>
        [Category("Field Configuration")]
        [DisplayName("Name")]
        [Description("The Name of the Column")]
        public string Name { get; set; }

        public override string ToString()
        {
            return $"{Name}, {Property}";
        }
    }
}