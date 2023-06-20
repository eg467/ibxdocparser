namespace ibxdocparser
{
    internal record Address(string Line1, string Line2, string City, string State, string Zip)
    {
         public override string ToString() =>
            $"{Line1}\r\n{Line2}\r\n{City}, {State} {Zip}";
    }
}

