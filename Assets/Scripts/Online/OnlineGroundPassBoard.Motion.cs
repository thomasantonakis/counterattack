using System;
using UnityEngine;
namespace CounterAttack.Online
{
    public sealed partial class OnlineGroundPassBoard
    {
        // One stream interval, with no future-frame buffer or extrapolation.
        private const float PresentationInterval=.05f;
        private struct MotionTrack{public Transform transform;public Vector3 start,target;}
        private MotionTrack[] presentationTracks=Array.Empty<MotionTrack>();
        private int presentationTrackCount;
        private Vector3 presentationBallStart,presentationBallTarget;
        private float presentationElapsed;
        private float presentationDuration;
        private bool presentationMoving,presentationHeld;
        private long presentationFrame=-1,presentationPublication=-1;
        private void ResetPresentationMotion(bool held)
        {presentationMoving=false;presentationHeld=held;}
        public void QueuePresentation(OnlineGroundPassPresentation frame) => QueuePresentation(frame, PresentationInterval);

        private void QueuePresentation(OnlineGroundPassPresentation frame, float duration)
        {
            if(!liveFullRoom){DisplayPresentation(frame);return;}
            if(!OnlineRoomSession.IsGuest || presentationHeld)return;
            if(frame.frameSequence==presentationFrame && frame.publicationSequence==presentationPublication)return;
            presentationFrame=frame.frameSequence;presentationPublication=frame.publicationSequence;
            presentationBallStart=displayedBall.position;presentationBallTarget=new Vector3(frame.ballX,frame.ballY,frame.ballZ);
            int count=frame.players?.Length??0;
            if(presentationTracks.Length<count)presentationTracks=new MotionTrack[count];
            presentationTrackCount=0;
            for(int i=0;i<count;i++)
            {
                var player=frame.players[i];int index=Array.FindIndex(tokens,token=>OnlineRollPublication.ActorId(token)==player.id);
                if(index<0)continue;
                var displayed=displayedTokens[index];presentationTracks[presentationTrackCount++]=new MotionTrack{transform=displayed,start=displayed.position,target=new Vector3(player.x,player.y,player.z)};
            }
            presentationElapsed=0;presentationDuration=duration;presentationMoving=true;
        }
        internal void AdvancePresentation(float seconds)
        {
            if(!presentationMoving || !OnlineRoomSession.IsGuest)return;
            if(float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            presentationElapsed=Mathf.Min(presentationDuration,presentationElapsed+seconds);float t=presentationElapsed/presentationDuration;
            displayedBall.position=Vector3.Lerp(presentationBallStart,presentationBallTarget,t);
            for(int i=0;i<presentationTrackCount;i++)
            {
                var track=presentationTracks[i];if(track.transform!=null)track.transform.position=Vector3.Lerp(track.start,track.target,t);
            }
            if(presentationElapsed>=presentationDuration)presentationMoving=false;
        }
    }
}
