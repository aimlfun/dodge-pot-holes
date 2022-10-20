namespace CarDodge.Settings
{
    /// <summary>
    /// Settings that relate to AI behaviour.
    /// </summary>
    internal class ConfigAI
    {
        /// <summary>
        /// This determines which output "neuron" is controlling the steering.
        /// </summary>
        internal const int c_steeringNeuron = 0;

        /// <summary>
        /// This determines which output "neuron" is controlling the speed.
        /// </summary>
        internal const int c_throttleNeuron = 1;

        /// <summary>
        /// Amplifies the steering returned by the AI. e.g. -1..1 => -15..15.
        /// </summary>
        public float SteeringAmplifier = 15;

        /// <summary>
        /// Amplifies the speed value returned by the AI. e.g. -1..1 => -10..10.
        /// </summary>
        public float SpeedAmplifier = 10;

        /// <summary>
        /// Learning requires us to create a number of cars, and mutate the worst 50%. 
        /// This happens repeatedly, resulting in a more fitting NN being selected.
        /// </summary>
        public int NumberOfAICarsToCreate { get; set; } = 20;

        /// <summary>
        /// 
        /// </summary>
        public bool ReduceVisionDepthAtSides { get; set; } = false;

        /// <summary>
        /// See FieldOfVisionStartInDegrees.
        /// </summary>
        private int _fieldOfVisionStartInDegrees = -140; // degrees

        /// <summary>
        ///     -45  0  45
        ///  -90 _ \ | / _ 90   <-- relative to direction of car, hence + angle car is pointing.
        ///   ^ this
        /// </summary>
        public int FieldOfVisionStartInDegrees
        {
            get { return _fieldOfVisionStartInDegrees; }
            set
            {
                if (value > FieldOfVisionStopInDegrees) FieldOfVisionStopInDegrees = value;

                _fieldOfVisionStartInDegrees = value;
            }
        }

        /// <summary>
        /// See FieldOfVisionStopInDegrees.
        /// </summary>
        private int _fieldOfVisionStopInDegrees = 140; // degrees

        /// <summary>
        ///     -45  0  45
        ///  -90 _ \ | / _ 90   <-- relative to direction of car, hence + angle car is pointing.
        ///                ^ this
        /// </summary>
        public int FieldOfVisionStopInDegrees
        {
            get { return _fieldOfVisionStopInDegrees; }

            set
            {
                if (value < FieldOfVisionStartInDegrees) FieldOfVisionStartInDegrees = value;

                _fieldOfVisionStopInDegrees = value;
            }
        }

        /// <summary>
        /// Do we check for 5 e.g. -90,-45,0,+45,+90, or just -45,0,45? etc.
        /// It will divide the field of view by this amount. 
        //              (3)  
        ///     (2) -45  0  45 (4)
        /// (1)  -90 _ \ | / _ 90  (5)  <-- # sample points = 5.
        /// </summary>
        public int SamplePoints { get; set; } = 17;//27

        /// <summary>
        /// See DepthOfVisionInPixels.
        /// </summary>
        private int _depthOfVisionInPixels = 120; // px

        /// <summary>
        /// ##########
        /// 
        ///    ¦    }
        ///    ¦    } how far the AI looks ahead
        ///    ¦    }
        ///   (o)  car
        /// </summary>
        public int DepthOfVisionInPixels
        {
            get { return _depthOfVisionInPixels; }

            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(value));

                _depthOfVisionInPixels = value;
            }
        }

        /// <summary>
        /// Subtracts the 2 angles and divides by sample point.
        /// </summary>
        internal float VisionAngleInDegrees
        {
            get
            {
                return (SamplePoints == 1) ? 0 : (float)(FieldOfVisionStopInDegrees - FieldOfVisionStartInDegrees) / (SamplePoints - 1);
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        internal ConfigAI()
        {
        }
    }
}
