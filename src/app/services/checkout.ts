import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface MyCheckoutAsset {
  checkoutID: number;
  assetID: number;
  assetName: string;
  category: string;
  serialNumber: string;
  checkoutDate: string;
  expectedReturnDate: string;
  isOverdue: boolean;
}

export interface CheckoutAuditRecord {
  auditLogID: number;
  action: string;
  assetID?: number;
  assetName: string;
  performedByEmail: string;
  timestamp: string;
  details: string;
}

@Injectable({
  providedIn: 'root',
})
export class CheckoutService {
  private apiUrl = 'https://localhost:7175/api/checkouts';

  constructor(private http: HttpClient) {}

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token') || localStorage.getItem('asset_tracker_token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  getMyAssets(): Observable<MyCheckoutAsset[]> {
    return this.http.get<MyCheckoutAsset[]>(`${this.apiUrl}/my-assets`, { headers: this.getAuthHeaders() });
  }

  returnAsset(assetID: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/return/${assetID}`, {}, { headers: this.getAuthHeaders() });
  }

  getAuditTrail(): Observable<CheckoutAuditRecord[]> {
    return this.http.get<CheckoutAuditRecord[]>(`${this.apiUrl}/audit-trail`, { headers: this.getAuthHeaders() });
  }
}
