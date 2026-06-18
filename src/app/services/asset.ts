import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Asset } from '../models/asset.model';
import { AuditTrail } from '../models/audit-trail.model';

@Injectable({
  providedIn: 'root'
})
export class AssetService {
  // Update this port to match your exact .NET backend URL from Swagger
  private apiUrl = 'https://localhost:7175/api/assets';

  constructor(private http: HttpClient) {}

  // Helper method to attach your stored JWT token badge to HTTP requests
  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token') || localStorage.getItem('asset_tracker_token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  // 1. GET ALL
  getAssets(): Observable<Asset[]> {
    return this.http.get<Asset[]>(this.apiUrl, { headers: this.getAuthHeaders() });
  }

  // 2. GET BY ID
  getAssetById(id: number): Observable<Asset> {
    return this.http.get<Asset>(`${this.apiUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  // 3. CREATE (Admin/Manager Only)
  createAsset(asset: any): Observable<any> {
    return this.http.post(this.apiUrl, asset, { headers: this.getAuthHeaders() });
  }

  addAsset(asset: Asset): Observable<Asset> {
    return this.createAsset(asset);
  }

  // 4. UPDATE (Admin/Manager Only)
  updateAsset(id: number, asset: Asset): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, asset, { headers: this.getAuthHeaders() });
  }

  // 5. DELETE (Admin/Manager Only)
  deleteAsset(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  // 6. ASSIGN ASSET TO EMPLOYEE
  assignAsset(
    assetId: number,
    employeeEmail: string,
    durationDays: number = 14,
    employeeFullName?: string,
    expectedReturnDate?: string
  ): Observable<any> {
    const payload = {
      assetID: assetId,
      employeeEmail: employeeEmail,
      durationDays: durationDays,
      employeeFullName,
      expectedReturnDate
    };
    return this.http.post(`${this.apiUrl.replace('/assets', '/checkouts')}/assign`, payload, { headers: this.getAuthHeaders() });
  }

  // 7. RETURN ASSET TO INVENTORY
  returnAsset(assetId: number): Observable<any> {
    return this.http.post(`${this.apiUrl.replace('/assets', '/checkouts')}/return/${assetId}`, {}, { headers: this.getAuthHeaders() });
  }

  adminOverrideReturn(assetId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${assetId}/admin-return`, {}, { headers: this.getAuthHeaders() });
  }

  restoreAsset(assetId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${assetId}/restore`, {}, { headers: this.getAuthHeaders() });
  }

  setMaintenanceStatus(assetId: number, setMaintenance: boolean): Observable<any> {
    return this.http.put(`${this.apiUrl}/${assetId}/maintenance`, { setMaintenance }, { headers: this.getAuthHeaders() });
  }

  // 8. FETCH CENTRAL SECURITY AUDIT FEED (Admin Only)
  getAuditTrail(action?: string, fromDate?: string, toDate?: string): Observable<AuditTrail[]> {
    let params = new HttpParams();
    if (action && action !== 'All') {
      params = params.set('action', action);
    }
    if (fromDate) {
      params = params.set('fromDate', fromDate);
    }
    if (toDate) {
      params = params.set('toDate', toDate);
    }

    return this.http.get<AuditTrail[]>(`${this.apiUrl.replace('/assets', '/checkouts')}/audit-trail`, {
      headers: this.getAuthHeaders(),
      params,
    });
  }

  // 9. FETCH EXECUTIVE DASHBOARD METRICS
  getDashboardMetrics(): Observable<any> {
    return this.http.get(`${this.apiUrl}/dashboard-metrics`, { headers: this.getAuthHeaders() });
  }
}
