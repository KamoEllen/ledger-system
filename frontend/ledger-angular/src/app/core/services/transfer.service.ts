import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateTransferRequest, TransferDto, TransferResultDto } from '../models/transfer.models';
import { PagedResponse } from '../models/wallet.models';

@Injectable({ providedIn: 'root' })
export class TransferService {
  private readonly base = `${environment.apiUrl}/transfers`;

  constructor(private http: HttpClient) {}

  createTransfer(req: CreateTransferRequest): Observable<TransferResultDto> {
    const idempotencyKey = crypto.randomUUID();
    const headers = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    return this.http.post<TransferResultDto>(this.base, req, { headers });
  }

  getTransfer(id: string): Observable<TransferDto> {
    return this.http.get<TransferDto>(`${this.base}/${id}`);
  }

  getTransfers(page = 1, pageSize = 20): Observable<PagedResponse<TransferDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<TransferDto>>(this.base, { params });
  }
}
