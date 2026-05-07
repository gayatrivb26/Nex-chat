import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponse, Conversation, Message, PagedResult } from '../models';

const API_BASE = '/api/v1';

@Injectable({ providedIn: 'root' })
export class ChatApiService {
  constructor(private readonly http: HttpClient) {}

  getConversations(page = 1, pageSize = 30): Observable<PagedResult<Conversation>> {
    return this.http
      .get<ApiResponse<PagedResult<Conversation>>>(`${API_BASE}/conversations`, {
        params: { page, pageSize }
      })
      .pipe(map(requireData));
  }

  getMessages(conversationId: string, take = 50): Observable<Message[]> {
    return this.http
      .get<ApiResponse<Message[]>>(`${API_BASE}/messages/${conversationId}`, {
        params: { take }
      })
      .pipe(map(requireData));
  }
}

function requireData<T>(response: ApiResponse<T>): T {
  if (!response.success || !response.data) {
    throw new Error(response.message ?? 'Request failed');
  }
  return response.data;
}
