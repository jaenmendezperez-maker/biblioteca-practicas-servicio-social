export interface Usuario {
  id: number;
  nombreCompleto: string;
  email: string;
  rol: number | string;
  fechaRegistro: string;
  activo: boolean;
}