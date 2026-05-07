import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class MediaDeviceService {
  enumerateDevices(): Promise<MediaDeviceInfo[]> {
    return navigator.mediaDevices.enumerateDevices();
  }

  getUserMedia(audio = true, video = false): Promise<MediaStream> {
    return navigator.mediaDevices.getUserMedia({ audio, video });
  }

  getDisplayMedia(): Promise<MediaStream> {
    return navigator.mediaDevices.getDisplayMedia({ video: true, audio: false });
  }
}
