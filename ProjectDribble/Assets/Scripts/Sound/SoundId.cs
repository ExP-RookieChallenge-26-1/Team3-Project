namespace DefaultNamespace
{
    public enum SoundId
    {
        None = 0,

        // Ball
        BallBounce = 1,
        BallGroundBounce = 2,
        BallReleased = 3,
        BallRespawn = 4,

        // Block
        BlockBreak = 5,

        // Gauge
        GaugeSegmentFilled = 6,

        // Laser
        LaserCharge = 7,
        LaserFire = 8,

        // Player / Ceiling
        PlayerHit = 9,
        CeilingHit = 10,
        CeilingBreak = 11,

        // UI
        UIClick = 12,

        // Game State
        GameOver = 13,
        StageClear = 14,

        // BGM
        StageBGM = 15,

        // 16 is intentionally unused because legacy scene data still contains this value.
        TitleBGM = 17,
        GameplayBGM = 18,

        // Ending
        EndingNextGlitch = 19,
        EndingGaugeErrorTick = 20,
        EndingPopupNoise = 21,
        EndingQuestionAppear = 22,
        EndingErrorVideoStart = 23,
        EndingBgm = 24
    }
}
