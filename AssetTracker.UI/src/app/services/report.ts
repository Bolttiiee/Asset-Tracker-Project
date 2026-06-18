import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ReportFilters {
  fromDate?: string;
  toDate?: string;
  category?: string;
  assetId?: number;
  userId?: number;
}

export interface CheckoutHistoryRow {
  checkoutID: number;
  checkoutDate: string;
  assetName: string;
  category: string;
  serialNumber: string;
  userFullName: string;
  userEmail: string;
  returnDate?: string;
  status: string;
}

export interface ReportDropdownAsset {
  id: number;
  name: string;
  serialNumber: string;
}

export interface ReportDropdownUser {
  id: number;
  name: string;
  email: string;
}

@Injectable({
  providedIn: 'root',
})
export class ReportService {
  private apiUrl = 'https://localhost:7175/api/reports';

  constructor(private http: HttpClient) {}

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token') || localStorage.getItem('asset_tracker_token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  private buildQueryParams(filters: ReportFilters): URLSearchParams {
    const params = new URLSearchParams();

    if (filters.fromDate) params.append('fromDate', filters.fromDate);
    if (filters.toDate) params.append('toDate', filters.toDate);
    if (filters.category && filters.category !== 'All') params.append('category', filters.category);
    if (typeof filters.assetId === 'number') params.append('assetId', String(filters.assetId));
    if (typeof filters.userId === 'number') params.append('userId', String(filters.userId));

    return params;
  }

  getCheckoutHistory(filters: ReportFilters): Observable<CheckoutHistoryRow[]> {
    const query = this.buildQueryParams(filters).toString();
    const suffix = query ? `?${query}` : '';
    return this.http.get<CheckoutHistoryRow[]>(`${this.apiUrl}/checkout-history${suffix}`, { headers: this.getAuthHeaders() });
  }

  downloadExcel(filters: ReportFilters): Observable<Blob> {
    const query = this.buildQueryParams(filters).toString();
    const suffix = query ? `?${query}` : '';
    return this.http.get(`${this.apiUrl}/checkout-history/excel${suffix}`, {
      headers: this.getAuthHeaders(),
      responseType: 'blob'
    });
  }

  downloadPdf(filters: ReportFilters): Observable<Blob> {
    const query = this.buildQueryParams(filters).toString();
    const suffix = query ? `?${query}` : '';
    return this.http.get(`${this.apiUrl}/checkout-history/pdf${suffix}`, {
      headers: this.getAuthHeaders(),
      responseType: 'blob'
    });
  }

  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/categories`, { headers: this.getAuthHeaders() });
  }

  getAssets(): Observable<ReportDropdownAsset[]> {
    return this.http.get<ReportDropdownAsset[]>(`${this.apiUrl}/assets`, { headers: this.getAuthHeaders() });
  }

  getUsers(): Observable<ReportDropdownUser[]> {
    return this.http.get<ReportDropdownUser[]>(`${this.apiUrl}/users`, { headers: this.getAuthHeaders() });
  }
}
