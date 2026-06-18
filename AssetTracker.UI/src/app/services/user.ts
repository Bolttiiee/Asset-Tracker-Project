import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UserSummary {
  userID: number;
  fullName: string;
  email: string;
  department: string;
  employeeID: string;
  phoneNumber?: string;
  role: string;
  accountStatus: string;
  registrationDate: string;
  lastLoginDate?: string;
  rejectionReason?: string;
}

export interface UserCheckoutItem {
  checkoutID: number;
  assetName: string;
  serialNumber: string;
  checkoutDate: string;
  expectedReturnDate: string;
  isOverdue: boolean;
}

export interface UserDetails extends UserSummary {
  approvedBy?: number;
  approvalRejectionDate?: string;
  currentCheckouts: UserCheckoutItem[];
}

export interface UserFilters {
  search?: string;
  department?: string;
  role?: string;
  status?: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = 'https://localhost:7175/api/users';

  constructor(private http: HttpClient) {}

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token') || localStorage.getItem('asset_tracker_token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  private buildQueryParams(filters?: UserFilters): URLSearchParams {
    const params = new URLSearchParams();
    if (!filters) {
      return params;
    }

    if (filters.search) params.append('search', filters.search);
    if (filters.department) params.append('department', filters.department);
    if (filters.role) params.append('role', filters.role);
    if (filters.status) params.append('status', filters.status);

    return params;
  }

  getAllUsers(filters?: UserFilters): Observable<UserSummary[]> {
    const query = this.buildQueryParams(filters).toString();
    const suffix = query ? `?${query}` : '';
    return this.http.get<UserSummary[]>(`${this.apiUrl}${suffix}`, { headers: this.getAuthHeaders() });
  }

  getUserById(id: number): Observable<UserDetails> {
    return this.http.get<UserDetails>(`${this.apiUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  approveUser(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${id}/approve`, {}, { headers: this.getAuthHeaders() });
  }

  rejectUser(id: number, rejectionReason: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/${id}/reject`,
      { rejectionReason },
      { headers: this.getAuthHeaders() }
    );
  }

  updateUserStatus(id: number, activate: boolean): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}/status`, { activate }, { headers: this.getAuthHeaders() });
  }

  updateUserRole(id: number, newRole: string): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}/role`, { newRole }, { headers: this.getAuthHeaders() });
  }

  resetUserPassword(id: number): Observable<{ message: string; temporaryPassword: string }> {
    return this.http.post<{ message: string; temporaryPassword: string }>(
      `${this.apiUrl}/${id}/reset-password`,
      {},
      { headers: this.getAuthHeaders() }
    );
  }

}
