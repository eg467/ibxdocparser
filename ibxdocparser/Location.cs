namespace ibxdocparser
{
    internal record Location(string? Name = null, double Latitude = 0, double Longitude = 0, Address? Address = null, string? Phone = null, bool? InNetwork = null)
    {
        public override string ToString() => $"{Name} ({Address})";
    }
}

