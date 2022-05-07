using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Undos;

namespace BII.WasaBii.Undo {
    public class UndoException : Exception {
        public enum UndoInvocationType { Undo, Redo }
        
        public readonly UndoInvocationType InvocationType;
        public readonly IReadOnlyList<SymmetricOperationDebugInfo> DebugInfo;

        public UndoException(
            Exception cause,
            UndoInvocationType invocationType,
            IReadOnlyList<SymmetricOperationDebugInfo> debugInfo
        ) : base(messageFor(cause, invocationType, debugInfo)) =>
            (this.DebugInfo, this.InvocationType) = (debugInfo, invocationType);

        private static string messageFor(
            Exception cause, 
            UndoInvocationType invocationType, 
            IEnumerable<SymmetricOperationDebugInfo> debugInfo
        ) {
            var formattedDebugInfo = string.Join("\n", debugInfo.Select(
                d => $"  in {d.CallerMemberName} (at {formatSourceFilePath(d.SourceFilePath)}:{d.SourceLineNumber})"
            ));
            return $"Exception in symmetric operation during {invocationType}: {cause.Message}\n" +
                $"{formattedDebugInfo}\n---------------------\n{cause.StackTrace}";
        }

        private static string formatSourceFilePath(string path) {
            var pathParts = path.Split('\\');
            var inUnityProject = pathParts.SkipWhile(p => p != "Assets");
            return string.Join("/", inUnityProject);
        }
            
    }
}