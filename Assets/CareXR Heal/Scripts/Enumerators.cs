using UnityEngine;

[SerializeField]
public enum GUIComponentType {
    Text,
    Button,
    Material,
    MeshRenderer,
    Generic

}





[SerializeField]
public enum ColorFormat {
    Grayscale,
    RGB,
    Unknown

}

[SerializeField]
public enum LogType {
    Info,
    Warning,
    Error,
    Fatal,
    Exception

}

[SerializeField]
public enum DetectionMode {
    OneShot,
    Passive,
    Timing

}

[SerializeField]
public enum SessionState { 
    Connecting,
    Disconnected,
    Initialized,
    Connected,
    Running
}

[SerializeField]
public enum ExerciseType {
    Panoramic,
    Model,

}

[SerializeField]
public enum PanoramicExercise {
    PointOfInterest,
    Recognition,
    LearningEye,
    LearningController,


}


