using JetBrains.Annotations;

namespace Xpand.Extensions.WindowManager{
    [System.Flags][PublicAPI]
    public enum MsgBoxStyle{
        /// <summary>
        /// OK button only (default). This member is equivalent to the Visual Basic constant <see langword="vbOKOnly" />.</summary>
        OkOnly = 0,

        /// <summary>
        /// OK and Cancel buttons. This member is equivalent to the Visual Basic constant <see langword="vbOKCancel" />.</summary>
        OkCancel = 1,

        /// <summary>
        /// Abort, Retry, and Ignore buttons. This member is equivalent to the Visual Basic constant <see langword="vbAbortRetryIgnore" />.</summary>
        AbortRetryIgnore = 2,

        /// <summary>
        /// Yes, No, and Cancel buttons. This member is equivalent to the Visual Basic constant <see langword="vbYesNoCancel" />.</summary>
        YesNoCancel = AbortRetryIgnore | OkCancel, // 0x00000003

        /// <summary>
        /// Yes and No buttons. This member is equivalent to the Visual Basic constant <see langword="vbYesNo" />.</summary>
        YesNo = 4,

        /// <summary>
        /// Retry and Cancel buttons. This member is equivalent to the Visual Basic constant <see langword="vbRetryCancel" />.</summary>
        RetryCancel = YesNo | OkCancel, // 0x00000005

        /// <summary>Critical message. This member is equivalent to the Visual Basic constant <see langword="vbCritical" />.</summary>
        Critical = 16, // 0x00000010

        /// <summary>Warning query. This member is equivalent to the Visual Basic constant <see langword="vbQuestion" />.</summary>
        Question = 32, // 0x00000020

        /// <summary>Warning message. This member is equivalent to the Visual Basic constant <see langword="vbExclamation" />.</summary>
        Exclamation = Question | Critical, // 0x00000030

        /// <summary>Information message. This member is equivalent to the Visual Basic constant <see langword="vbInformation" />.</summary>
        Information = 64, // 0x00000040

        /// <summary>First button is default. This member is equivalent to the Visual Basic constant <see langword="vbDefaultButton1" />.</summary>
        DefaultButton1 = 0,

        /// <summary>Second button is default. This member is equivalent to the Visual Basic constant <see langword="vbDefaultButton2" />.</summary>
        DefaultButton2 = 256, // 0x00000100

        /// <summary>Third button is default. This member is equivalent to the Visual Basic constant <see langword="vbDefaultButton3" />.</summary>
        DefaultButton3 = 512, // 0x00000200

        /// <summary>Application modal message box. This member is equivalent to the Visual Basic constant <see langword="vbApplicationModal" />.</summary>
        ApplicationModal = 0,

        /// <summary>System modal message box. This member is equivalent to the Visual Basic constant <see langword="vbSystemModal" />.</summary>
        SystemModal = 4096, // 0x00001000

        /// <summary>Help text. This member is equivalent to the Visual Basic constant <see langword="vbMsgBoxHelp" />.</summary>
        MsgBoxHelp = 16384, // 0x00004000

        /// <summary>Right-aligned text. This member is equivalent to the Visual Basic constant <see langword="vbMsgBoxRight" />.</summary>
        MsgBoxRight = 524288, // 0x00080000

        /// <summary>Right-to-left reading text (Hebrew and Arabic systems). This member is equivalent to the Visual Basic constant <see langword="vbMsgBoxRtlReading" />.</summary>
        MsgBoxRtlReading = 1048576, // 0x00100000

        /// <summary>Foreground message box window. This member is equivalent to the Visual Basic constant <see langword="vbMsgBoxSetForeground" />.</summary>
        MsgBoxSetForeground = 65536, // 0x00010000
    }
}