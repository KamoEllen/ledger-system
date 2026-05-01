import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminUserDto, UpdateUserRoleRequest } from '../models/admin.models';
import { WalletDto, PagedResponse } from '../models/wallet.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly base = `${environment.apiUrl}/admin`;

  constructor(private http: HttpClient) {}

  getUsers(page = 1, pageSize = 20): Observable<PagedResponse<AdminUserDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<AdminUserDto>>(`${this.base}/users`, { params });
  }

  getUser(id: string): Observable<AdminUserDto> {
    return this.http.get<AdminUserDto>(`${this.base}/users/${id}`);
  }

  updateUserRole(id: string, req: UpdateUserRoleRequest): Observable<AdminUserDto> {
    return this.http.patch<AdminUserDto>(`${this.base}/users/${id}/role`, req);
  }

  freezeWallet(id: string): Observable<WalletDto> {
    return this.http.post<WalletDto>(`${this.base}/wallets/${id}/freeze`, {});
  }

  unfreezeWallet(id: string): Observable<WalletDto> {
    return this.http.post<WalletDto>(`${this.base}/wallets/${id}/unfreeze`, {});
  }
}
