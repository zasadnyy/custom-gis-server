//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WcfService
{
    using System;
    using System.Collections.Generic;
    
    public partial class Station
    {
        public Station()
        {
            this.Lands = new HashSet<Land>();
            this.Prices = new HashSet<Price>();
            this.Persons = new HashSet<Person>();
            this.Services = new HashSet<Service>();
        }
    
        public int ID { get; set; }
        public int Company_ID { get; set; }
        public string Street { get; set; }
        public int HouseNo { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string Coordinates { get; set; }
        public string Description { get; set; }
    
        public virtual Company Company { get; set; }
        public virtual ICollection<Land> Lands { get; set; }
        public virtual ICollection<Price> Prices { get; set; }
        public virtual ICollection<Person> Persons { get; set; }
        public virtual ICollection<Service> Services { get; set; }
    }
}
