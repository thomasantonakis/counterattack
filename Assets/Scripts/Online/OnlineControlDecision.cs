using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    /// <summary>Detached input state; capturing it never traverses roster, poses, logs or statistics.</summary>
    public sealed class OnlineControlDecision
    {
        public readonly string matchId,decisionId,owner,stage,pauseRequester;
        public readonly long revision,sharedPauseRevision,homeAcceptedSequence,awayAcceptedSequence,homePauseSequence,awayPauseSequence;
        public readonly bool connectionHeld,isSharedPaused,keeperActionBlocked,substitutionsAvailable,shootoutSelecting;
        public readonly bool introPending,homeIntroReady,awayIntroReady;
        public readonly bool resumePending,homeResumeReady,awayResumeReady;
        public readonly long substitutionReviewId;
        public readonly bool homeSubstitutionConfirmed, awaySubstitutionConfirmed;
        public readonly bool homeKeeperReplacement,awayKeeperReplacement,homeForcedKeeperSubstitution,awayForcedKeeperSubstitution,homeEmergencyKeeper,awayEmergencyKeeper;
        public readonly IReadOnlyList<string> legalActions;
        private OnlineControlDecision(OnlineGroundPassState source)
        {
            matchId=source.matchId;decisionId=source.decisionId;owner=source.owner;stage=source.stage;pauseRequester=source.pauseRequester;
            revision=source.revision;sharedPauseRevision=source.sharedPauseRevision;
            homeAcceptedSequence=source.homeAcceptedSequence;awayAcceptedSequence=source.awayAcceptedSequence;
            homePauseSequence=source.homePauseSequence;awayPauseSequence=source.awayPauseSequence;
            connectionHeld=source.connectionHeld;isSharedPaused=source.isSharedPaused;
            introPending=source.introPending;homeIntroReady=source.homeIntroReady;awayIntroReady=source.awayIntroReady;
            resumePending=source.resumePending;homeResumeReady=source.homeResumeReady;awayResumeReady=source.awayResumeReady;
            keeperActionBlocked=source.keeperActionBlocked;substitutionsAvailable=source.substitutionsAvailable;
            substitutionReviewId=source.substitutionReview?.id ?? 0;
            homeSubstitutionConfirmed=source.substitutionReview?.homeConfirmed==true;
            awaySubstitutionConfirmed=source.substitutionReview?.awayConfirmed==true;
            shootoutSelecting=source.shootout?.selecting==true;
            homeKeeperReplacement=source.homeKeeperReplacement;awayKeeperReplacement=source.awayKeeperReplacement;
            homeForcedKeeperSubstitution=source.homeForcedKeeperSubstitution;awayForcedKeeperSubstitution=source.awayForcedKeeperSubstitution;
            homeEmergencyKeeper=source.homeEmergencyKeeper;awayEmergencyKeeper=source.awayEmergencyKeeper;
            legalActions=Array.AsReadOnly(source.legalActions==null?Array.Empty<string>():(string[])source.legalActions.Clone());
        }
        public static OnlineControlDecision Capture(OnlineGroundPassState source)=>source==null?null:new OnlineControlDecision(source);
        public OnlineGroundPassMessage Command(long sequence,string action)=>new()
        {kind="command",matchId=matchId,decisionId=decisionId,revision=revision,sequence=sequence,actionId=action};
    }
}
