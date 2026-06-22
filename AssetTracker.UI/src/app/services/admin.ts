import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map } from 'rxjs';

export interface OverdueReminderResult {
  overdueCount: number;
  remindersSent: number;
  remindersFailed: number;
  digestSent: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private apiUrl = 'https://localhost:7175/api/admin';

  constructor(private http: HttpClient) {}

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token') || localStorage.getItem('asset_tracker_token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  runOverdueReminders(): Observable<OverdueReminderResult> {
    return this.http.post<any>(
      `${this.apiUrl}/run-overdue-reminders`,
      {},
      { headers: this.getAuthHeaders() }
    ).pipe(
      map(res => ({
        overdueCount: res.overdueCount,
        remindersSent: res.emailsSent ?? res.remindersSent,
        remindersFailed: res.emailsFailed ?? res.remindersFailed,
        digestSent: res.adminDigestSent ?? res.digestSent
      }))
    );
  }
}
