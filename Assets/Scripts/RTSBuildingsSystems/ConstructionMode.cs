namespace RTS.Buildings
{
    /// <summary>
    /// Defines different construction modes for buildings and walls.
    /// </summary>
    public enum ConstructionMode
    {
        /// <summary>
        /// Building is constructed instantly upon placement (no delay).
        /// </summary>
        Instant,

        /// <summary>
        /// Building takes time to construct but doesn't require workers.
        /// Construction progresses automatically over time.
        /// </summary>
        Timed,

        /// <summary>
        /// Building is constructed segment by segment over time without workers.
        /// Each segment completes one at a time automatically.
        /// </summary>
        SegmentWithoutWorkers,

        /// <summary>
        /// Building is constructed segment by segment with worker assignment.
        /// Each segment requires one worker, and player can assign multiple workers
        /// to different segments to speed up construction.
        /// </summary>
        SegmentWithWorkers
    }
}
