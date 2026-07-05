import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../config/environment.token';
import { ApiResult } from './api-result';

@Injectable({
  providedIn: 'root',
})
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);

  get<T>(path: string): Observable<ApiResult<T>> {
    return this.http.get<ApiResult<T>>(this.buildUrl(path));
  }

  post<TRequest, TResponse>(path: string, body: TRequest): Observable<ApiResult<TResponse>> {
    return this.http.post<ApiResult<TResponse>>(this.buildUrl(path), body);
  }

  put<TRequest, TResponse>(path: string, body: TRequest): Observable<ApiResult<TResponse>> {
    return this.http.put<ApiResult<TResponse>>(this.buildUrl(path), body);
  }

  delete<T>(path: string): Observable<ApiResult<T>> {
    return this.http.delete<ApiResult<T>>(this.buildUrl(path));
  }

  private buildUrl(path: string): string {
    const normalizedPath = path.startsWith('/') ? path : `/${path}`;

    return `${this.apiBaseUrl}${normalizedPath}`;
  }
}
