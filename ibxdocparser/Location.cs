namespace ibxdocparser
{
    internal record Location
    {
        public string Name { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Address? Address { get; set; }
        public string? Phone { get; set; }

        public override string ToString() => $"{Name} ({Address}) ({Phone})";
    }

}

