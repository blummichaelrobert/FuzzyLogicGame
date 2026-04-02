// ============================================================
//  NullRenderer.cs
//  Placeholder renderer — does nothing, compiles cleanly.
//  Swap for ConsoleRenderer once Layer 3 is built.
//  To verify engine output during development, uncomment
//  the Console.WriteLine inside Render().
// ============================================================
 
namespace BiodomeAres1.Rendering
{
    public class NullRenderer : IRenderer
    {
        public void Initialise() { }
        public void Teardown()   { }
 
        public void Render(Engine.GameSnapshot snapshot)
        {
            // Uncomment to watch engine ticks in the terminal:
            // Console.WriteLine(
            //     $"Tick {snapshot.CurrentTick} | " +
            //     $"Stress {snapshot.FuzzyResult?.SystemStress:F2} | " +
            //     $"Crew {snapshot.CrewHealth:F1}% | " +
            //     $"Eco {snapshot.EcosystemIntegrity:F1}% | " +
            //     $"Dome {snapshot.DomeIntegrity:F1}%");
        }
    }
}