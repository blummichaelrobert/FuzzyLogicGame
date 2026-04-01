// ============================================================
//  IRenderer.cs
//  The single contract between the game engine and any
//  rendering layer — console, GUI, or otherwise.
//
//  The engine calls Render(snapshot) once per tick.
//  It never knows or cares what happens next.
// ============================================================
 
namespace BiodomeAres1.Rendering
{
    public interface IRenderer
    {
        /// <summary>
        /// Draw the current game state from an immutable snapshot.
        /// Called by GameLoop after each tick and after each
        /// player input that changes state.
        /// </summary>
        void Render(Engine.GameSnapshot snapshot);
 
        /// <summary>
        /// Called once at startup to initialise the display
        /// (clear console, set window size, hide cursor, etc).
        /// </summary>
        void Initialise();
 
        /// <summary>
        /// Called on shutdown to restore terminal to clean state.
        /// </summary>
        void Teardown();
    }
}