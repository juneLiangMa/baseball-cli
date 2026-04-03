using System;
using System.Collections.Generic;

namespace BaseballCli.Tests
{
    /// <summary>
    /// Main test runner. Executes both unit and integration tests.
    /// </summary>
    public class TestRunner
    {
        public static void RunAllTests()
        {
            var suites = new List<(string Name, Action<TestContext> Suite)>
            {
                ("Unit Tests", RunUnitTests),
                ("Integration Tests", RunIntegrationTests),
            };

            Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           Baseball CLI - Complete Test Suite                ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

            var context = new TestContext();

            foreach (var (name, suite) in suites)
            {
                try
                {
                    suite(context);
                }
                catch (Exception ex)
                {
                    context.TestsFailed++;
                    Console.WriteLine($"\n[ERROR] {name} suite failed: {ex.Message}\n");
                }
            }

            PrintSummary(context);
        }

        private static void RunUnitTests(TestContext context)
        {
            Console.WriteLine("┌────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ UNIT TESTS - Core Simulation Logic                         │");
            Console.WriteLine("└────────────────────────────────────────────────────────────┘\n");

            try
            {
                var tests = new SimulationEngineTests();
                tests.RunAllTests();
                context.TestsPassed += 7;
            }
            catch (Exception ex)
            {
                context.TestsFailed += 1;
                Console.WriteLine($"Unit tests failed: {ex.Message}");
            }
        }

        private static void RunIntegrationTests(TestContext context)
        {
            Console.WriteLine("\n┌────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ INTEGRATION TESTS - End-to-End Workflows                  │");
            Console.WriteLine("└────────────────────────────────────────────────────────────┘\n");

            try
            {
                var tests = new IntegrationTests();
                tests.RunAllTests();
                context.TestsPassed += 8;
            }
            catch (Exception ex)
            {
                context.TestsFailed += 1;
                Console.WriteLine($"Integration tests failed: {ex.Message}");
            }
        }

        private static void PrintSummary(TestContext context)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                        TEST SUMMARY                        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

            var total = context.TestsPassed + context.TestsFailed;
            var passRate = total > 0 ? (context.TestsPassed * 100 / total) : 0;

            Console.WriteLine($"Total Tests: {total}");
            Console.WriteLine($"✓ Passed:    {context.TestsPassed}");
            Console.WriteLine($"✗ Failed:    {context.TestsFailed}");
            Console.WriteLine($"Pass Rate:   {passRate}%\n");

            if (context.TestsFailed == 0)
            {
                Console.WriteLine("[SUCCESS] All tests passed! 🎉\n");
            }
            else
            {
                Console.WriteLine("[FAILURE] Some tests failed. Review output above.\n");
            }
        }

        private class TestContext
        {
            public int TestsPassed { get; set; }
            public int TestsFailed { get; set; }
        }
    }
}
