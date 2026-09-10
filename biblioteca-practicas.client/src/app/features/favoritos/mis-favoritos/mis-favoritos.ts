import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Libro } from '../../../core/models/libro.model';
import { FavoritoService } from '../../../core/services/favorito.service';
import { SidebarComponent } from '../../../shared/components/sidebar/sidebar.component';
import { SidebarService } from '../../../core/services/sidebar.service';

@Component({
  selector: 'app-mis-favoritos',
  imports: [CommonModule, RouterLink, SidebarComponent],
  templateUrl: './mis-favoritos.html',
  styleUrl: './mis-favoritos.scss',
})
export class MisFavoritosComponent implements OnInit {
  libros: Libro[] = [];
  cargando = true;
  error = '';

  private coloresPortada = ['bg-marino-700', 'bg-ambar-600', 'bg-marino-900', 'bg-marino-400'];

  constructor(
    private favoritoService: FavoritoService,
    public sidebarService: SidebarService
  ) {}

  ngOnInit(): void {
    this.favoritoService.getLibrosFavoritos().subscribe({
      next: (data) => {
        this.libros = data;
        this.cargando = false;
      },
      error: (err) => {
        console.error(err);
        this.error = 'No se pudieron cargar tus favoritos.';
        this.cargando = false;
      }
    });
  }

  colorPortada(id: number): string {
    return this.coloresPortada[id % this.coloresPortada.length];
  }

  iniciales(titulo: string): string {
    return titulo.split(' ').filter(w => w.length > 0).slice(0, 2).map(w => w[0].toUpperCase()).join('');
  }

  urlArchivo(ruta: string): string {
    const partes = ruta.split('/').filter(p => p);
    // partes[0] = "uploads", partes[1] = "libros"/"portadas"/"preview", partes[2] = nombreArchivo
    const carpeta = partes[1];
    const nombreArchivo = partes[2];
    return `http://localhost:5250/api/archivos/${carpeta}/${nombreArchivo}`;
  }
}
