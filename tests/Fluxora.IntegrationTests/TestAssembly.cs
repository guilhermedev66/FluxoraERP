using Xunit;

// Quartz configures a process-wide logging provider. Running independent WebApplicationFactory
// instances concurrently can let one fixture dispose that provider while another host starts.
// Individual concurrency tests still issue genuinely parallel HTTP requests with Task.WhenAll.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
