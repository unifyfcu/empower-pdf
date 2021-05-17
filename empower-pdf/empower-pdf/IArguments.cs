namespace empower_pdf
{
    public interface IArguments
    {
        string SourcePath { get; }
        string DestinationPath { get; }
        string WatermarkText { get; }
        bool Verbose { get; }
    }
}
