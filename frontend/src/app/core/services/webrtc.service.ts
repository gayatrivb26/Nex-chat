import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class WebRTCService {
  private peer?: RTCPeerConnection;

  createPeerConnection(iceServers: RTCIceServer[]): RTCPeerConnection {
    this.peer?.close();
    this.peer = new RTCPeerConnection({ iceServers });
    return this.peer;
  }

  async createOffer(stream: MediaStream): Promise<RTCSessionDescriptionInit> {
    const peer = this.ensurePeer();
    stream.getTracks().forEach(track => peer.addTrack(track, stream));
    const offer = await peer.createOffer();
    await peer.setLocalDescription(offer);
    return offer;
  }

  async acceptRemoteDescription(description: RTCSessionDescriptionInit): Promise<void> {
    await this.ensurePeer().setRemoteDescription(description);
  }

  async addIceCandidate(candidate: RTCIceCandidateInit): Promise<void> {
    await this.ensurePeer().addIceCandidate(candidate);
  }

  close(): void {
    this.peer?.close();
    this.peer = undefined;
  }

  private ensurePeer(): RTCPeerConnection {
    if (!this.peer) throw new Error('Peer connection has not been created');
    return this.peer;
  }
}
