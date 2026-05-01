export interface CreateTransferRequest {
  sourceWalletId: string;
  destinationWalletId: string;
  amount: number;
  currency: string;
  description?: string;
}

export interface TransferDto {
  id: string;
  sourceWalletId: string;
  destinationWalletId: string;
  amount: number;
  currency: string;
  status: 'Pending' | 'Completed' | 'Failed';
  description: string | null;
  idempotencyKey: string;
  createdAt: string;
  completedAt: string | null;
}

export interface TransferResultDto {
  transfer: TransferDto;
  sourceBalanceAfter: number;
  destinationBalanceAfter: number;
}
