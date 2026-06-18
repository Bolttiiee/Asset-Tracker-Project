import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface RegisterRequest {
  fullName: string;
  email: string;
  employeeID: string;
  department: string;
  password: string;
  phoneNumber?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // Replace this port with your exact .NET backend running port
  private apiUrl = 'https://localhost:7175/api/auth';

  constructor(private http: HttpClient) {}

  register(user: RegisterRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, user);
  }

  login(credentials: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        if (response && response.token) {
          // Store both the token and role under the keys the rest of the app reads.
          localStorage.setItem('token', response.token);
          localStorage.setItem('asset_tracker_token', response.token);

          if (response.user) {
            localStorage.setItem('user_profile', JSON.stringify(response.user));

            const userRole = response.user.role ?? response.user.userRole ?? '';
            if (userRole) {
              localStorage.setItem('userRole', userRole);
            }
          }

          if (response.role && !localStorage.getItem('userRole')) {
            localStorage.setItem('userRole', response.role);
          }
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('asset_tracker_token');
    localStorage.removeItem('userRole');
    localStorage.removeItem('user_profile');
  }

  isLoggedIn(): boolean {
    return !!(localStorage.getItem('token') || localStorage.getItem('asset_tracker_token'));
  }

  checkEmailAvailable(email: string): Observable<{ available: boolean }> {
    return this.http.get<{ available: boolean }>(`${this.apiUrl}/check-email?email=${encodeURIComponent(email)}`);
  }

  checkEmployeeIDAvailable(employeeID: string): Observable<{ available: boolean }> {
    return this.http.get<{ available: boolean }>(`${this.apiUrl}/check-employee-id?employeeID=${encodeURIComponent(employeeID)}`);
  }

  forgotPassword(email: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(token: string, newPassword: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/reset-password`, { token, newPassword });
  }

  changePassword(newPassword: string): Observable<any> {
    const token = localStorage.getItem('token') || localStorage.getItem('asset_tracker_token');
    return this.http.post(
      `${this.apiUrl}/change-password`,
      { newPassword },
      { headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' } }
    );
  }
}