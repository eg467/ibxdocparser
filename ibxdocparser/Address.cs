namespace ibxdocparser
{
    internal record Address
    {
        public string? Line1 { get; set; }
        public string? Line2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? Zip { get; set; }
        public string? County { get; set; }

        public override string ToString() =>
            $"{Line1}\r\n{Line2}\r\n{City}, {State} {Zip}\r\n{County} County\r\b{Country}";
    }
}

