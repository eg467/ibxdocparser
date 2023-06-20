namespace ibxdocparser
{
    internal record IbxProfile
    {
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public long? Id { get; set; }
        public string? Gender { get; set; }
        public string? BoardCertified { get; set; }
        public Experience[] Education { get; set; }
        public Experience[] Residencies { get; set; }
        public string? ImageUri { get; set; }
        public Location[] GroupAffiliations { get; set; } = Array.Empty<Location>();
        public Location[] HospitalAffiliations { get; set; } = Array.Empty<Location>();
        public Location[] Locations { get; set; } = Array.Empty<Location>();
    }

}
