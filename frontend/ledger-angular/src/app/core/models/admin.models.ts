export interface AdminUserDto {
  id: string;
  email: string;
  role: string;
  createdAt: string;
}

export interface UpdateUserRoleRequest {
  role: string;
}
