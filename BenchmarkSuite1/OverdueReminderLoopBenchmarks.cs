using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AssetTracker.API.Data;
using AssetTracker.API.Models;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.VSDiagnostics;

namespace AssetTracker.API.Benchmarks
{
    [CPUUsageDiagnoser]
    public class OverdueReminderLoopBenchmarks
    {
        // Representative per-send SMTP round-trip latency. Each SendOverdueReminderAsync opens a
        // brand-new connection, so connect + TLS + auth + send dominates. 50 ms is a conservative
        // cloud/LAN estimate; public providers such as Gmail are frequently 100-300 ms, which
        // only widens the gap between the two approaches.
        private const int SimulatedSendLatencyMs = 50;
        private const int MaxDegreeOfParallelism = 4;
        [Params(10, 50, 100)]
        public int OverdueCount { get; set; }

        private List<Checkout> _overdueCheckouts = null!;
        [GlobalSetup]
        public void Setup()
        {
            _overdueCheckouts = Enumerable.Range(1, OverdueCount).Select(i => new Checkout { CheckoutID = i, AssetID = i, UserID = 2, UserFullName = $"User {i}", UserEmail = $"user{i}@test.com", CheckoutTimestamp = DateTime.UtcNow.AddDays(-30), ExpectedReturnDate = DateTime.UtcNow.Date.AddDays(-5), Status = "Active", Asset = new Asset { AssetID = i, Name = $"Asset {i}", SerialNumber = $"SN{i:000}", Category = "Laptop", Status = "Checked Out" } }).ToList();
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
            return new ApplicationDbContext(options);
        }

        // Models SmtpEmailService.SendOverdueReminderAsync: a fresh connection + network round
        // trip per recipient.
        private static Task SendOneAsync(Checkout checkout) => Task.Delay(SimulatedSendLatencyMs);
        private static AuditTrail BuildAudit(Checkout checkout, DateTime today) => new AuditTrail
        {
            Action = "EMAIL_REMINDER",
            AssetID = checkout.AssetID,
            AssetName = checkout.Asset?.Name ?? "Unknown",
            PerformedByEmail = "system@automated",
            Timestamp = DateTime.UtcNow,
            Details = $"Overdue reminder email sent to {checkout.UserFullName} ({checkout.UserEmail}). " + $"Days overdue: {(today - checkout.ExpectedReturnDate.Date).Days}"};
        // ── BEFORE: current sequential loop (mirrors ProcessOverdueRemindersAsync) ──
        [Benchmark(Baseline = true)]
        public async Task<int> Sequential()
        {
            using var context = CreateContext();
            var today = DateTime.UtcNow.Date;
            var emailsSent = 0;
            foreach (var checkout in _overdueCheckouts)
            {
                await SendOneAsync(checkout);
                context.AuditTrails.Add(BuildAudit(checkout, today));
                emailsSent++;
            }

            await context.SaveChangesAsync();
            return emailsSent;
        }

        // ── AFTER: bounded parallelism + split-pass (DbContext stays single-threaded) ──
        [Benchmark]
        public async Task<int> BoundedParallel()
        {
            using var context = CreateContext();
            var today = DateTime.UtcNow.Date;
            // Pass 1: send concurrently with a bounded degree of parallelism. No DbContext here,
            // so nothing touches the non-thread-safe context from multiple threads.
            var sent = new ConcurrentBag<Checkout>();
            await Parallel.ForEachAsync(_overdueCheckouts, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (checkout, ct) =>
            {
                await SendOneAsync(checkout);
                sent.Add(checkout);
            });
            // Pass 2: write audit rows sequentially against the single DbContext.
            foreach (var checkout in sent)
            {
                context.AuditTrails.Add(BuildAudit(checkout, today));
            }

            await context.SaveChangesAsync();
            return sent.Count;
        }
    }
}