namespace Library.Models
{
    public class Address
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string Neighborhood { get; set; }
        public string DeliveryAddress { get; set; }
        public Int32? BuildingNumber { get; set; }
        public Int32? FloorNumber { get; set; }
        public Int32? ApartmentNumber { get; set; }
        public int? PostCode { get; set; }
        public int PhoneNumber { get; set; }
        public string FullName { get; set; }


    }
}
