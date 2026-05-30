        /*
         for every x coord all the values in the column:
         spliting coord between ground and air = BaseSurfaceLevel +
          (GetNoise(x) - 0.5 ) <- to center noise is [0, 1] we want [-0.5, 0.5]
           * 2 <- normalize [-0.5, 0.5] -> [-1, 1]
           *  Amplitude <- scale the value from [-1, 1] to [-Amplitude, Amplitude]

         second pass
         for every y coord all the values in the row;
         */

        /*
        e =    1 * noise(Frequency * nx, Frequency * ny);
        +  1 * FractalGain * noise(Frequency * Lacunarity * nx, Frequency * Lacunarity * ny);
        + 1 * FractalGain ^ 2 * noise(Frequency * Lacunarity ^ 2 * nx, Frequency * Lacunarity ^ 2 * ny);
        ...;
        (example has 3 fractal octaves)
        e = e / (1 + 1 * FractalGain + 1 * FractalGain ^ 2);
        in order to make flat valleys we need to somehow
        1.push middle elevations down into valleys
        or 2. pull middle elevations up towards mountain peaks
        but in my case since i mesaerue 0-1 in each place as 0 <- closer to ground closer to air -> 1 this is useful for creating smaller or bigger connected caves

        noise = Math.pow(e * FudgeFactor, Exponent) i could use other functions aside pow to create

        if i want to create the mountains i only
        */

            // fix a (0, 0) coordinate issue wiht large offset
            // System.Random prng = new System.Random(_config.Seed);
            // int offsetX = prng.Next(10000, 100000); 
            // int offsetY = prng.Next(10000, 100000);
            //
            // if (prng.Next(0, 2) == 0) {
            //     offsetX *= -1; 
            // }
            //
            // if (prng.Next(0, 2) == 0) {
            //     offsetY *= -1; 
            // }



        public BiomeType BiomeType;
        
        [Header("Surface Generation")] 
        public int BaseSurfaceLevel = 150;
        
        public int AmplitudeY = 15;
        public float FrequencyY = 0.5f;
        
        public int AmplitudeX = 3;
        public float FrequencyX = 0.05f;
        public float FractalLacunarity = 2f;
        public float FractalGain = 0.5f;

        [Header("Cave Generation")]
        public float CaveFrequency = 0.07f;
        public float TunnelThickness = 0.2f;