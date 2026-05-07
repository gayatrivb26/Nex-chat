import { Injectable } from '@angular/core';
import { SignalrService } from './signalr.service';

@Injectable({ providedIn: 'root' })
export class CallSignalingService {
  constructor(private readonly signalr: SignalrService) {}

  initiateCall(conversationId: string, targetUserId: string, callType: 'audio' | 'video'): Promise<void> {
    return this.signalr.invoke('InitiateCall', { conversationId, targetUserId, callType });
  }

  sendOffer(callId: string, targetUserId: string, sdp: string): Promise<void> {
    return this.signalr.invoke('SendCallOffer', { callId, targetUserId, sdp, sdpType: 'offer' });
  }

  sendAnswer(callId: string, targetUserId: string, sdp: string): Promise<void> {
    return this.signalr.invoke('SendCallAnswer', { callId, targetUserId, sdp, sdpType: 'answer' });
  }

  sendIceCandidate(callId: string, targetUserId: string, candidate: RTCIceCandidateInit): Promise<void> {
    return this.signalr.invoke('SendIceCandidate', {
      callId,
      targetUserId,
      candidate: candidate.candidate,
      sdpMid: candidate.sdpMid,
      sdpMLineIndex: candidate.sdpMLineIndex
    });
  }

  endCall(callId: string): Promise<void> {
    return this.signalr.invoke('EndCall', callId);
  }

  rejectCall(callId: string): Promise<void> {
    return this.signalr.invoke('RejectCall', callId);
  }
}
